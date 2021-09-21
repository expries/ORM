using ORM.Application.Entities;
using ORM.Core.Database;

namespace ORM.Application.DbContexts
{
    public class ShopContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        
        public DbSet<Book> Books { get; set; }
    }
}