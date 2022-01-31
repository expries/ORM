using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class WithFk
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(3) Create and load an object with foreign key");
            Console.WriteLine("----------------------------------------------");

            var teacher = DbContext.GetById<Teacher>(0);

            var @class = new Class
            {
                ClassId = 1,
                Name = "Demonology 101",
                Teacher = teacher
            };

            DbContext.Save(@class);

            @class = DbContext.GetById<Class>(1);
            Console.WriteLine(@class.Teacher.FullName + " teaches " + @class.Name + ".");

            Console.WriteLine("\n");
        }
    }
}