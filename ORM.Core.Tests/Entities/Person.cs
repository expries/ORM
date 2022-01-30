using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ORM.Core.Tests.Entities
{
    public class Person
    {
        [Key]
        public int PersonId { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string FirstName { get; set; }
        
        [MaxLength(40)]
        [NotNull]
        public string LastName { get; set; }
    }
}