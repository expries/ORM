using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ORM.Application.Entities
{
    public class Author : Person
    {
        [Key]
        public int AuthorId { get; set; }

        [NotNull]
        public double Price { get; set; }
        
        public List<Book> Books { get; set; }
    }

    public class Person
    {
        [Key]
        public int PersonId { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string FirstName { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string LastName { get; set; }
    }
}