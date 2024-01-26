using Applicationlayer;
using securityclient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Transportlayer
{
    public class TransportLayerClient
    {
        public string PCNAME;
        public bool enable_QRcode = false;
        public IPAddress server = null;
        public Aes myAes = Aes.Create();
        static public Queue<string> MessageQueue = new Queue<string>();
        private int MessagePort;
        /// <summary>
        /// Init function of the transport layer
        /// </summary>
        /// <param name="name">The user-given name of the client.</param>
        /// <param name="BroadCastPort">The port the client will listen for broadcast. Default:7000</param>
        /// <param name="ConnectionRequestPort">The port the client will listen for connection requests.  Default:8005</param>
        /// <param name="MessagagingPort">The port that will be utilized when the setup is done for communication. Default:8001</param>
        /// <param name="BroadcastReplyPort">The instance that represents the port number will reply to the Broadcasting. Default: 8006</param>
        /// <returns>Void</returns>
        public void init(string name,int BroadCastPort,int ConnectionRequestPort, int MessagagingPort, int BroadCastReplyPort)
        {
            MessagePort = MessagagingPort;
            PCNAME = name;
            Thread UDP = new Thread(() => StartUdpListener(BroadCastPort, BroadCastReplyPort));
            Thread ReceiveConnect = new Thread(() => TcpReceive(ConnectionRequestPort));
            Thread TCPMain = new Thread(() => Setup(MessagagingPort));
            UDP.Start();
            ReceiveConnect.Start();
            TCPMain.Start();
            Debug.Log("key: " + Convert.ToBase64String(myAes.Key));
        }
        /// <summary>
        /// Starts up the Sender and Receiver handler. This is put in a different function for the non-blocking property
        /// </summary>
        /// <param name="Port">The instance that represents the port number tthe connection is setup on.</param>
        /// <returns>Void</returns>
        private void Setup(int Port)
        {
            while (server == null)
            {
                Thread.Sleep(500);
            }
            Debug.Log("Server setup");
            TcpClient inputTCP = new TcpClient(server.ToString(), Port); //default 8001
            // Create a cancellation token source
            var cts = new CancellationTokenSource();
            // Create a cancellation token from the source
            var token = cts.Token;
            Thread SenderThread = new Thread(() => HandleSenderClient(inputTCP, token));
            Thread Receiverthread = new Thread(() => HandleReceiverClient(inputTCP, cts));
            SenderThread.Start();
            Receiverthread.Start();
        }
        /// <summary>
        /// Starts the sender handler of the client
        /// </summary>
        /// <param name="tcpclient">The TcpClient that needs to be transmitted to.</param>
        /// <returns>Void</returns>
        private void HandleSenderClient(TcpClient tcpclient,CancellationToken token) //transport layer
        {
            Stream stream = tcpclient.GetStream();
            while (!token.IsCancellationRequested)
            {
                while (MessageQueue.Count > 0)
                {
                    try
                    {
                        Debug.Log("check for sending");
                        SecurityFunctionsClient.SecureSend(MessageQueue.Dequeue(), stream, myAes.Key);
                    }
                    catch (IOException e)
                    {
                        Debug.Log(e.Message);
                    }

                }
                Thread.Sleep(500);
            }
            Debug.Log("cancelationtoken received");
            Thread TCPMain = new Thread(() => Setup(MessagePort));
            TCPMain.Start();
        }
        /// <summary>
        /// Starts the receiving handler of the client
        /// </summary>
        /// <param name="tcpclient">The TcpClient that needs to be listened to.</param>
        /// <returns>Void</returns>
        private void HandleReceiverClient(TcpClient tcpclient, CancellationTokenSource cts)//transport layer
        {
            bool isRunning = true;
            try
            {
                while (isRunning)
                {
                    NetworkStream stream = tcpclient.GetStream();
                    string input = SecurityFunctionsClient.SecureReceive(stream, myAes.Key);
                    TypeContainer receivedContainer = JsonUtility.FromJson<TypeContainer>(input);
                    if (receivedContainer.JsonData == "Done")
                    {
                        isRunning = false;
                        Debug.Log("Done received");
                        server = null;
                        cts.Cancel();
                        stream.Close();
                        tcpclient.Close();
                    }
                    else if (input != null)
                    {
                        ApplicationLayerClient.AddObjectUnique(receivedContainer, tcpclient);
                        Debug.Log(input);
                    }
                    else
                    {
                        isRunning = false;
                        Debug.Log("Weird");
                    }

                }
            }
            catch (IOException e)
            {
                Debug.Log("Print error" + e);
                Debug.Log("out of infite receive");
            }
        }
        /// <summary>
        /// Reacts on the connection request
        /// </summary>
        /// <param name="Port">The instance that represents the port number the connection request needs to send to.</param>
        /// <returns>Void</returns>
        private void TcpReceive(int Port) //transport layer
        {
            TcpListener receive = new TcpListener(IPAddress.Any, Port);//default port: 8005
            receive.Start();
            while (true)
            {
                if (server == null)
                {
                    //Debug.Log("TCP receive is back online");
                    TcpClient newClient = receive.AcceptTcpClient();
                    Stream mystream = newClient.GetStream();
                    string received = SecurityFunctionsClient.SecureReceive(mystream, myAes.Key);
                    Debug.Log("Received: " + received);
                    if (received == "connect to me")
                    {
                        SecurityFunctionsClient.SecureSend("ok", mystream, myAes.Key);
                        Debug.Log("ok send");
                        IPEndPoint endPnt = (IPEndPoint)newClient.Client.RemoteEndPoint;
                        server = endPnt.Address;
                    }
                    else
                    {
                        Debug.Log("bad key used");
                    }
                }
            }
            
        }
        /// <summary>
        /// Starts the listener for UDP broadcasting
        /// </summary>
        /// <param name="Port">The instance that represents the port number the Broadcasting will be listening. Default: 7000</param>
        /// <param name="BroadcastReplyPort">The instance that represents the port number will reply to the Broadcasting. Default: 8006</param>
        /// <returns>Void</returns>
        private void StartUdpListener(int Port, int BroadcastReplyPort)
        {
            int listenPort = Port; //Default value = 7000;

            var listener = new UdpClient(listenPort) { EnableBroadcast = true };

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, listenPort);

            try
            {
                while (true)
                {
                    Debug.Log("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);
                    enable_QRcode = true;
                    TcpClient socketConnection = new TcpClient(groupEP.Address.ToString(), BroadcastReplyPort);
                    StreamWriter stream = new StreamWriter(socketConnection.GetStream(), Encoding.ASCII);
                    try
                    {
                        stream.WriteLine(PCNAME);
                        stream.Flush();
                        stream.Close();
                        socketConnection.Close();
                    }

                    catch (SocketException e)
                    {
                        Debug.Log(e);
                        stream.Close();
                        socketConnection.Close();
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.Log(e);
            }
            finally
            {
                listener.Close();
            }
        }
    }

}
