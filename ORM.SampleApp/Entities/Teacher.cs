using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ORM.Application.Entities
{
    public class Teacher : Person
    {
        public int Salary { get; set; }
        
        [Column("hdate")]
        public DateTime HireDate { get; set; }

        public virtual List<Class> Classes { get; private set; } = new List<Class>();
    }
}