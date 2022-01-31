using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ORM.Application.Entities
{
    public class Class
    {
        [Key]
        public int ClassId { get; set; }
        
        public string Name { get; set; }
        
        public virtual Teacher Teacher { get; set; }

        public virtual List<Student> Students { get; set; } = new List<Student>();
    }
}