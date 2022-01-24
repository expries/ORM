using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;

namespace ORM.Application.Show
{
    public static class GetEntity
    {
        public static void GetAuthor()
        {
            var dbContext = new ShopContext();
            
            var author = new Author
            {
                PersonId = 1,
                Interest = 10,
                Price = 100,
                FirstName = "Max",
                LastName = "Mustermann"
            };
            
            Console.WriteLine($"Saving author with id {author.PersonId}");
            dbContext.Save(author);

            Console.WriteLine($"Retrieving author with id {author.PersonId}");
            var savedAuthor = dbContext.GetById<Author>(author.PersonId);
            
            foreach (var property in savedAuthor.GetType().GetProperties())
            {
                object? value = property.GetValue(savedAuthor);
                Console.WriteLine($"{property.Name} = {value}");
            }
        }
    }
}