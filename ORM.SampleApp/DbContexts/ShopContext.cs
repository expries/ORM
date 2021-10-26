using System.Data;
using ORM.Application.Entities;
using ORM.Core;
using ORM.Core.Interfaces;

namespace ORM.Application.DbContexts
{
    public class ShopContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        public DbSet<Book> Books { get; set; }
        
        public ShopContext(ISqlDialect dialect, IDbConnection connection) : base(dialect, connection)
        {
        }
    }
}