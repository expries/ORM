using System;
using System.Linq;

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

            //Show.SaveObject.ShowBasic();
            //Show.SaveObject.ShowWithManyToOne();
            Show.SaveObject.ShowWithOneToMany();
            Show.SaveObject.ShowWithOneToMany();
            //Show.SaveObject.ShowWithManyToMany();

            //var authors = ctx.GetAll<Author>().ToList();
            // var products = ctx.GetAll<Product>().ToList();

            
            //var sellers = ctx.GetAll<Seller>().ToList();
            //var products = ctx.GetAll<Product>().ToList();

            //var products = ctx.Books.ToList();
            var x = ctx.Books.Count(x => x.Price > 100);
            int i = 0;
        }
    }
}