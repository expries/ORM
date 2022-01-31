using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Models.Tests.Entities
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public Class Class { get; set; }
    }
}