using System.Linq;
using ORM.Application.DbContexts;
using ORM.Application.Entities;
using ORM.Core;
using ORM.Postgres.Extensions;

namespace ORM.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            DbContext.Configure(options =>
            {
                options.UseStateTrackingCache();
                options.UsePostgres("Server=localhost;Port=5434;User Id=postgres;Password=postgres;");
            });

            var ctx = new ShopContext();
            ctx.EnsureCreated();
            

            Show.SaveObject.ShowBasic();
            Show.SaveObject.ShowWithManyToOne();
            Show.SaveObject.ShowWithOneToMany();
            Show.SaveObject.ShowWithManyToMany();
            
            Show.Linq.ShowToList();
            
            Show.GetEntity.GetAuthor();

            var authors = ctx.GetAll<Author>().ToList();
            var sellers = ctx.GetAll<Seller>().ToList();
            var products = ctx.GetAll<Product>().ToList();

            var books = ctx.Books.ToList();
            int x = ctx.Books.Count(x => x.Price > 100);
            int y = ctx.Books.Where(x => x.Price > 100).Count();
            double z = ctx.Books.Max(x => x.Price);
            int i = 0;
        }
    }
}