using ORM.Application.Entities;
using ORM.Core;
using ORM.Core.Interfaces;

namespace ORM.Application.DbContexts
{
    public class ShopContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = DbFactory.CreateDbSet<Product>();

        public DbSet<Book> Books { get; set; } = DbFactory.CreateDbSet<Book>();

        public ShopContext(ICommandBuilder dialect, ICache cache) : base(dialect, cache)
        {
        }

        protected ShopContext(ICommandBuilder dialect) : base(dialect)
        {
        }
    }
}