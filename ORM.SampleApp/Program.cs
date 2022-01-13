﻿using System;
using System.Linq;
using System.Reflection;
using ORM.Application.Entities;

namespace ORM.Application
{
    public class X
    {
        public Lazy<string> Name { get; set; }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var ctx = DbFactory.CreateDbContext();
            ctx.EnsureCreated();

            Show.SaveObject.ShowBasic();
            Show.SaveObject.ShowWithManyToOne();
            Show.SaveObject.ShowWithOneToMany();
            Show.SaveObject.ShowWithManyToMany();

            //var authors = ctx.GetAll<Author>().ToList();
            var books = ctx.GetAll<Book>().ToList();
            books.Select(b => b.Author).ToList();

            
            //var sellers = ctx.GetAll<Seller>().ToList();
            //var products = ctx.GetAll<Product>().ToList();
            
            Console.WriteLine();
        }
    }
}