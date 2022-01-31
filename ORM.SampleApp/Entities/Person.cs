using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORM.Application.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }
        
        [MaxLength(255)]
        public string FirstName { get; set; }
        
        [MaxLength(255)]
        [Column("Name")]
        public string LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
        
        public DateTime BirthDate { get; set; }
        
        /// <summary>Instance number counter.</summary>
        private static int _number = 1;
        
        [NotMapped]
        public int InstanceNumber { get; protected set; } = _number++;
        
        
    }
}