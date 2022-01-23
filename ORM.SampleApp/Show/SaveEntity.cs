using System.Collections.Generic;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class SaveObject
    {
        private static DbContext DbContext = DbFactory.CreateDbContext();
        
        public static void ShowBasic()
        {
            var product = new Product
            {
                ProductId = 1,
                Name = "my favourite book", 
                Price = 10, 
                Purchases = 5,
                Likes = 100,
            };
            
            DbContext.Save(product);
        }

        public static void ShowWithManyToOne()
        {
            var book = new Book
            {
                BookId = 1,
                Author = new Author 
                {
                    PersonId = 10,
                    Interest = 10,
                    Price = 100,
                    FirstName = "Alex new",
                    LastName = "Test"   
                },
                Likes = 3,
                Price = 300,
                Title = "My book new",
                Purchases = 30,
            };
            
            DbContext.Save(book.Author);
            DbContext.Save(book);
        }

        public static void ShowWithOneToMany()
        {
            var author = new Author
            {
                PersonId = 1,
                Interest = 10,
                Price = 100,
                FirstName = "Max",
                LastName = "Mustermann"
            };
            
            DbContext.Save(author);
            author.Books.ForEach(b => DbContext.Save(b));
        }

        public static void ShowWithManyToMany()
        {
            var product = new Product
            {
                Name = "my favourite book", 
                Price = 10, 
                Purchases = 5,
                Likes = 100,
                ProductId = 1,
                Sellers =  new List<Seller>
                {
                    new Seller
                    {
                        Id = 12,
                        Name = "Seller for book"
                    },
                    new Seller
                    {
                        Id = 13,
                        Name = "Seller for book"
                    }
                }
            };

            product.Sellers.ForEach(x => DbContext.Save(x));
            DbContext.Save(product);
        }
    }
}