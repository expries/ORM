using System.Collections.Generic;
using ORM.Core.Attributes;

namespace ORM.Application.Entities
{
    public class Book
    {
        public List<Author> Authors { get; set; }

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
    }
}