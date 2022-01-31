using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class ModifyObject
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(2) Load and modify object");
            Console.WriteLine("--------------------------");

            var teacher = DbContext.GetById<Teacher>(0);

            Console.WriteLine();
            Console.WriteLine("Salary for " + teacher.FirstName + " " + teacher.LastName + " is " + teacher.Salary + " Pesos.");

            Console.WriteLine("Give rise of 12000.");
            teacher.Salary += 12000;

            Console.WriteLine("Salary for " + teacher.FirstName + " " + teacher.LastName + " is now " + teacher.Salary + " Pesos.");

            DbContext.Save(teacher);

            Console.WriteLine("\n");
        }
    }
}