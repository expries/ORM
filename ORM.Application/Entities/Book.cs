using System.Collections.Generic;
using ORM.Infrastructure.Attributes;

namespace ORM.Application.Entities
{
    public class Book
    {
        [PrimaryKey]
        public int BookId { get; set; }
        
        [Unique]
        [MaxLength(40)]
        [NotNull]
        public string Title { get; set; }
        
        [NotNull]
        public double Price { get; set; }
        
        [NotNull]
        public int Purchases { get; set; }
        
        public int Likes { get; set; }
        
        public List<Author> Authors { get; set; }
    }
}