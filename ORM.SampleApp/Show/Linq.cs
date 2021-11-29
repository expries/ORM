using System;
using System.Linq;
using ORM.Application.Entities;

namespace ORM.Application.Show
{
    public static class Linq
    {
        public static void ShowToList()
        {
            var dbSet = Program.CreateDbSet<Product>();
            var products = dbSet.ToList();
            
            Console.WriteLine($"Found {products.Count} products:");
            
            foreach (var product in products)
            {
                Console.WriteLine($"Name: {product.Name}, Likes: {product.Likes}, Price: {product.Price} " +
                                  $"Purchases: {product.Purchases}, ProductId: {product.ProductId}, " +
                                  $"InsertedInStore: {product.InsertedInStore}");
            }
        }
    }
}