using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORM.Core.Models.Tests.Entities
{
    public class Author : Person
    {
        [Column("PriceColumn")]
        public double Price { get; set; }

        public virtual List<Book> Books { get; set; }
    }
}