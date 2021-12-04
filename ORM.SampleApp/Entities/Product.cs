using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using ORM.Core.Models.Attributes;

namespace ORM.Application.Entities
{
    public class Product
    {
        [Key]
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
        
        public virtual List<Seller> Sellers { get; set; }
    }
}