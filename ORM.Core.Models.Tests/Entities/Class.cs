using System.ComponentModel.DataAnnotations;

namespace ORM.Core.Models.Tests.Entities
{
    public class Class
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }

        public virtual Course Course { get; set; }
    }
}