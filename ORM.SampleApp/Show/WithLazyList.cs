using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class WithLazyList
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(6) Use Lazy loading for student list");
            Console.WriteLine("-------------------------------------");

            var @class = DbContext.GetById<Class>(1);
            @class.Students.Add(DbContext.GetById<Student>(1));
            @class.Students.Add(DbContext.GetById<Student>(2));

            DbContext.Save(@class);

            @class = DbContext.GetById<Class>(1);

            Console.WriteLine("Students in " + @class.Name + ":");
            
            foreach(var student in @class.Students)
            {
                Console.WriteLine(student.FullName);
            }

            Console.WriteLine("\n");
        }
    }
}