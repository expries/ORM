using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class WithFkList
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(4) Load teacher and show classes");
            Console.WriteLine("---------------------------------");

            var teacher = DbContext.GetById<Teacher>(0);

            Console.WriteLine(teacher.FullName + " teaches:");

            foreach(var @class in teacher.Classes)
            {
                Console.WriteLine(@class.Name);
            }

            Console.WriteLine("\n");
        }
    }
}