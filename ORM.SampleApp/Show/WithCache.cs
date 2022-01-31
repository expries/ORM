using System;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class WithCache
    {
        private static DbContext _dbContext = new SchoolContext();
        
        public static void Show()
        {
            Console.WriteLine("(6) Cache demonstration");
            Console.WriteLine("-----------------------");

            Console.WriteLine("\nWith cache:");
            ShowInstances();
            
            DbContext.Configure(options => options.UseNoCache());
            _dbContext = new SchoolContext();

            Console.WriteLine("\nWithout cache:");
            ShowInstances();

            DbContext.Configure(options => options.UseEntityCache());
            _dbContext = new SchoolContext();
        }
        
        /// <summary>
        /// Shoes instances
        /// </summary>
        private static void ShowInstances()
        {
            for(int i = 0; i < 7; i++)
            {
                var teacher = _dbContext.GetById<Teacher>(0);
                Console.WriteLine("Object [" + teacher.PersonId + "] instance no: " + teacher.InstanceNumber);
            }
        }
    }
}