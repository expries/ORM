using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ORM.Application.Entities
{
    public class Seller
    {
        [Key]
        public int Id { get; set; }
        
        public string? Name { get; set; }
        
        public List<Product> Product { get; set; }
    }
}