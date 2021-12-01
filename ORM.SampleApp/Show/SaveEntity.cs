using System;
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
                    PersonId = 1,
                    Interest = 10,
                    Price = 100,
                    FirstName = "Max",
                    LastName = "Mustermann"
                },
                Likes = 10,
                Price = 100,
                Title = "My book",
                Purchases = 10,
                BookId = 1
            };
            
            dbContext.Save(book);
            Console.WriteLine();
        }

        public static void ShowSeller()
        {
            var dbContext = Program.CreateDbContext();
            var seller = new Seller { Name = "Seller #1", Id = 1 };
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
                LastName = "Mustermann"
            };
            dbContext.Save(author);
        }
    }
}