using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ORM.Application.Entities
{
    public class Author
    {
        [Key]
        public int AuthorId { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string Name { get; set; }
        
        [NotNull]
        public double Price { get; set; }
        
        public List<Book> Books { get; set; }
    }
}