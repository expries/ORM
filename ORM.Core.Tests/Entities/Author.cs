using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ORM.Core.Tests.Entities
{
    public class Author : PersonOfInterest
    {
        [NotNull]
        public double Price { get; set; }
        
        public virtual List<Book> Books { get; set; }
    }
}