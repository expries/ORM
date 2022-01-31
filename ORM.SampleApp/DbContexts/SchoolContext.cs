using ORM.Application.Entities;
using ORM.Core;

namespace ORM.Application.DbContexts
{
    public class SchoolContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        
        public DbSet<Class> Classes { get; set; }
        
        public DbSet<Teacher> Teachers { get; set; }
        
        public DbSet<Course> Courses { get; set; }
    }
}