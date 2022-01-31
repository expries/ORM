using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ORM.Application.Entities
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        
        public string Name { get; set; }
        
        public virtual Teacher Teacher { get; set; }

        public virtual List<Student> Students { get; set; } = new List<Student>();
    }
}