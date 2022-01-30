using ORM.Core.Tests.Entities;

namespace ORM.Core.Tests.DbContexts
{
    public class TestContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public DbSet<Book> Books { get; set; }
    }
}