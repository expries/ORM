using System;
using System.Linq;
using ORM.Application.DbContexts;

namespace ORM.Application.Show
{
    public static class Linq
    {
        public static void ShowToList()
        {
            var dbContext = new ShopContext();
            var products = dbContext.Products.ToList();
            
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