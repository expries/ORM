using System.Linq;
using Npgsql;
using ORM.Application.Entities;
using ORM.Core;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;

namespace ORM.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            //var connection = new SQLiteConnection("Data Source=test.sqlite;Version=3;");
            var connection = new NpgsqlConnection("Server=localhost;Port=5432;User Id=postgres;Password=postgres;");
            connection.Open();
            
            var typeMapper = new PostgresDataTypeMapper();
            var dialect = new PostgresSqlDialect(typeMapper);
            var dbContext = new DbContext(connection, dialect);

            var lazyLoader = new LazyLoader(connection, dialect);

            //var sellers = dbContext.GetAll<Product>().ToList();
            var books = dbContext.GetAll<Product>().ToList();

            return;

            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator, lazyLoader);
            var dbSet = new DbSet<Book>(provider);

            var book = new Book();
            book.Title = "My Book :)";
            dbContext.Save(book);
        }
    }
}