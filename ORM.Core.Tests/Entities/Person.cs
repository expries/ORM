using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Tests.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
    }
}