using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class WithMToN
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(5) Create and load an object with m:n");
            Console.WriteLine("--------------------------------------");

            var course = new Course
            {
                CourseId = 0,
                Name = "Demons 1",
                Teacher = DbContext.GetById<Teacher>(0)
            };

            // Save first student
            var student = new Student
            {
                PersonId = 1,
                LastName = "Aalo",
                FirstName = "Alice",
                BirthDate = new DateTime(1990, 1, 12),
                Grade = 1,
                Class = new Class
                {
                    Name = "ClassA",
                    ClassId = 1,
                    Teacher = course.Teacher
                }
            };
            
            DbContext.Save(student);
            course.Students.Add(student);

            // Save second student
            student = new Student
            {
                PersonId = 2,
                LastName = "Bumblebee",
                FirstName = "Bernard",
                BirthDate = new DateTime(1991, 9, 23),
                Grade = 2,
                Class = student.Class
            };
            
            DbContext.Save(student);
            course.Students.Add(student);

            // Save course
            DbContext.Save(course);

            // Output course
            course = DbContext.GetById<Course>(0);
            Console.WriteLine("Students in " + course.Name + ":");
            
            foreach(var studentOfCourse in course.Students)
            {
                Console.WriteLine(studentOfCourse.FullName);
            }

            Console.WriteLine("\n");
        }
    }
}