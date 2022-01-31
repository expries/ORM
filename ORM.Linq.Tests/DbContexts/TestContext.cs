using ORM.Core;
using ORM.Linq.Tests.Entities;

namespace ORM.Linq.Tests.DbContexts
{
    public class TestContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public DbSet<Book> Books { get; set; }
    }
}