using System.Collections.Generic;
using ORM.Infrastructure.Attributes;

namespace ORM.Application.Entities
{
    public class Author
    {
        [PrimaryKey]
        public int AuthorId { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string Name { get; set; }
        
        [NotNull]
        public double Price { get; set; }
        
        public List<Book> Books { get; set; }
    }
}