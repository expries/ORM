using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Tests.Entities
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        
        public string Name { get; set; }
        
        public double Price { get; set; }
        
        public int Purchases { get; set; }
        
        public int Likes { get; set; }
        
        public DateTime InsertedInStore { get; set; }
        
        public virtual List<Seller> Sellers { get; set; }
    }
}