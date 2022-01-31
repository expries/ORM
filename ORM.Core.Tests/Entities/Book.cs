using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Tests.Entities
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }
        
        public string Title { get; set; }
        
        public double Price { get; set; }
        
        public int Purchases { get; set; }
        
        public int Likes { get; set; }

        public virtual Author Author { get; set; }
    }
}