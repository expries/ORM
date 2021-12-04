using System.Collections.Generic;
using ORM.Application.Entities;

namespace ORM.Application.Show
{
    public static class SaveObject
    {
        public static void ShowProduct()
        {
            var dbContext = Program.CreateDbContext();
            var product = new Product { Name = "my favourite book", Price = 10, Purchases = 5 };
            dbContext.Save(product);
        }
        
        public static void ShowBook()
        {
            var dbContext = Program.CreateDbContext();
            var book = new Book
            {
                Author = new Author
                {
                    PersonId = 3,
                    Interest = 10,
                    Price = 100,
                    FirstName = "Alex",
                    LastName = "Test"   
                },
                Likes = 3,
                Price = 300,
                Title = "My book 3",
                Purchases = 30,
                BookId = 3
            };
            
            dbContext.Save(book);
        }

        public static void ShowSeller()
        {
            var dbContext = Program.CreateDbContext();
            var seller = new Seller
            {
                Name = "Seller #1", 
                Id = 1, 
                Products = new List<Product>()
            };
            dbContext.Save(seller);
        }

        public static void ShowAuthor()
        {
            var dbContext = Program.CreateDbContext();
            var author = new Author
            {
                PersonId = 1,
                Interest = 10,
                Price = 100,
                FirstName = "Max",
                LastName = "Mustermann",
                Books = new List<Book>
                {
                     new Book
                     {
                         Likes = 10,
                         Price = 100,
                         Title = "My book",
                         Purchases = 10,
                         BookId = 1
                     },
                     new Book
                     {
                         Likes = 20,
                         Price = 200,
                         Title = "My book 2",
                         Purchases = 20,
                         BookId = 2
                     }
                }
            };
            dbContext.Save(author);
        }
    }
}