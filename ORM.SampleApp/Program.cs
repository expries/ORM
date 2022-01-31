using ORM.Application.Show;
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
            
            EnsureCreated.Show();
            InsertObject.Show();
            ModifyObject.Show();
            WithFk.Show();
            WithFkList.Show();
            WithMToN.Show();
            WithLazyList.Show();
            WithCache.Show();
            WithQuery.Show();
        }
    }
}