using System;
using ORM.Infrastructure.Attributes;

namespace ORM.Application.Entities
{
    public class Product
    {
        [PrimaryKey]
        public int ProductId { get; set; }
        
        [Unique]
        [MaxLength(40)]
        [NotNull]
        public string Name { get; set; }
        
        [NotNull]
        public double Price { get; set; }
        
        [NotNull]
        public int Purchases { get; set; }
        
        public int Likes { get; set; }
        
        public DateTime InsertedInStore { get; set; }
    }
}