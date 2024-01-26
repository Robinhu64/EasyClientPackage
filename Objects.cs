using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace objects
{
    public class Car
    {
        public string Model;
        public int Year;
        public string Color;

        public Car(string model, int year, string color)
        {
            Model = model;
            Year = year;
            Color = color;
            
        }
        public string CarInfo()
        {
            return "The " + Color + " " + Model + " is made in " + Year.ToString();
        }

    }
    public class Book
    {
        public string Author;
        public int Pages;

        public Book(string author, int pages)
        {
            Author = author;
            Pages = pages;
        }

    }

}
