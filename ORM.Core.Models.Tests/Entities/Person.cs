using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ORM.Core.Models.Attributes;

namespace ORM.Core.Models.Tests.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }
        
        [Unique]
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }
        
        [MaxLength(100)]
        public string LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}