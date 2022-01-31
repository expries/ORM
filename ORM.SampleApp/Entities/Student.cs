using System.Collections.Generic;

namespace ORM.Application.Entities
{
    public class Student : Person
    {
        public int Grade { get; set; }
        
        public virtual Class Class { get; set; }
        
        public virtual List<Course> Courses { get; set; } = new List<Course>();
    }
}