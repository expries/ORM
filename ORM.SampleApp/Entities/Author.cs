using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ORM.Application.Entities
{
    public class Author : PersonOfInterest
    {
        [NotNull]
        public double Price { get; set; }
        
        public List<Book> Books { get; set; }
    }
}