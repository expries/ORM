using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Models.Tests.Entities
{
    public class DuplicateKeyPerson : Person
    {
        [Key]
        public int DuplicateKey { get; set; }
        
        public string Name { get; set; }
    }
}