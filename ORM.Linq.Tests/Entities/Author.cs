using System.Collections.Generic;

namespace ORM.Linq.Tests.Entities
{
    public class Author : Person
    {
        public double Price { get; set; }
        
        public virtual List<Book> Books { get; set; }
    }
}