using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class InsertObject
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(1) Insert object");
            Console.WriteLine("-----------------");

            var teacher = new Teacher
            {
                PersonId = 0,
                FirstName = "Jerry",
                LastName = "Mouse",
                BirthDate = new DateTime(1970, 8, 18),
                HireDate = new DateTime(2015, 6, 20),
                Salary = 50000
            };

            DbContext.Save(teacher);

            Console.WriteLine("\n");
        }
    }
}