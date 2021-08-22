using ORM.Application.Entities;
using ORM.Infrastructure.Database;

namespace ORM.Application.DbContexts
{
    public class ShopContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        
        public DbSet<Book> Books { get; set; }
    }
}