using System;
using System.Linq;
using ORM.Application.DbContexts;

namespace ORM.Application.Show
{
    public static class WithQuery
    {
        private static readonly SchoolContext SchoolContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(7) Query demonstration");
            Console.WriteLine("-----------------------");

            Console.WriteLine("Students with grade > 1:");
            
            foreach(var student in SchoolContext.Students.Where(x => x.Grade > 1))
            {
                Console.WriteLine(student.FullName);
            }

            Console.WriteLine("\nStudents with grade > 1 or firstname starts with 'Al':");
            
            foreach(var student in SchoolContext.Students.Where(x => x.Grade > 1 || x.FirstName.StartsWith("Al")))
            {
                Console.WriteLine(student.FullName);
            }
            
            Console.WriteLine("\nStudents with grade > 1 or lastname ends with with 'lo':");
            
            foreach(var student in SchoolContext.Students.Where(x => x.Grade > 1 || x.LastName.EndsWith("lo")))
            {
                Console.WriteLine(student.FullName);
            }

            Console.WriteLine("\nShow all persons:");
            
            foreach(var student in SchoolContext.Students)
            {
                Console.WriteLine(student.FullName + " (" + student.GetType().Name + ")");
            }
            
            Console.WriteLine($"There are {SchoolContext.Students.Count()} students in total.");

            Console.WriteLine($"There are {SchoolContext.Students.Count(x => x.Grade > 1)} students with a grade over 1");

            Console.WriteLine("Getting first student with first name starting with an 'A':");
            var studentA = SchoolContext.Students.FirstOrDefault(x => x.FirstName.StartsWith('A'));
            Console.WriteLine(studentA?.FullName);
        }
    }
}