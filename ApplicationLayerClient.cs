using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Transportlayer;

namespace Applicationlayer
{
    
    public class ApplicationLayerClient
    {
        static List<ObjectMessage> ObjectList = new List<ObjectMessage>();
        /// <summary>
        /// Will add an extra object to the list, if ther already exist an object of the same type from that sender the oldest one will be removed.
        /// </summary>
        /// <param name="TypeContainer">The TypeContainer that contains the message that needs to be uniqily added. </param>
        /// <param name="TcpClient">The TcpClient of the sender of the container</param>
        /// <returns>Void</returns>
        static public void AddObjectUnique(TypeContainer Message, TcpClient newClient)
        {
            IPEndPoint IP = (IPEndPoint)newClient.Client.RemoteEndPoint;
            for (int i = 0; i < ObjectList.Count; i++)
            {
                if ((ObjectList[i].container.TypeName == Message.TypeName) && (ObjectList[i].ip.ToString() == IP.Address.ToString()))
                {
                    Debug.Log("check of container name" + ObjectList[i].container.TypeName.ToString() + " and " + Message.TypeName.ToString());
                    ObjectList.RemoveAt(i);
                    break;
                }

            }
            ObjectList.Add(new ObjectMessage(IP.Address, Message));
        }
        /// <summary>
        /// Retrieves the object from the list that is in the client
        /// </summary>
        /// <param name="T">The class that wants to be retrieved. </param>
        /// <returns>The object from the list, the IP-addres from the sender of the object</returns>
        public (T, IPAddress) ObjectReceive<T>() //application layer
        {
            Debug.Log("Objctlist length: " + ObjectList.Count.ToString());
            for (int i = 0; i < ObjectList.Count; i++)
            {
                TypeContainer receivedContainer = ObjectList[i].container;
                if (receivedContainer.TypeName == typeof(T).FullName)
                {
                    T testvar = JsonUtility.FromJson<T>(receivedContainer.JsonData);
                    IPAddress IP = ObjectList[i].ip;
                    ObjectList.RemoveAt(i);
                    return (testvar, IP);
                }
            }
            return (default(T), null);
        }
        /// <summary>
        /// Puts an object on the Queue to transmit it
        /// </summary>
        /// <param name="T data">The class that wants to be transmitted. </param>
        /// <returns>Void</returns>
        public void ObjectSend<T>(T data) //appication layer
        {
            TypeContainer container = new TypeContainer
            {
                TypeName = data.GetType().FullName,
                JsonData = JsonUtility.ToJson(data)
            };
            string typeIdentifierJson = JsonUtility.ToJson(container);
            Debug.Log(typeIdentifierJson);
            TransportLayerClient.MessageQueue.Enqueue(typeIdentifierJson); //messagueue if from transport layer
        }
    }
   
    public class TypeContainer
    {
        public string TypeName;
        public string JsonData;
    }
    public class ObjectMessage
    {
        public IPAddress ip;
        public TypeContainer container;

        public ObjectMessage(IPAddress ipAddress, TypeContainer typecontainer)
        {
            ip = ipAddress;
            container = typecontainer;
        }
    }
}