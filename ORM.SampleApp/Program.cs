using Npgsql;
using ORM.Core;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;

namespace ORM.Application
{
    class Program
    {
        private const string ConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=postgres;";
        
        public static DbContext CreateDbContext()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var dialect = new PostgresSqlDialect(typeMapper);
            var dbContext = new DbContext(connection, dialect);
            connection.Open();
            return dbContext;
        }

        public static DbSet<T> CreateDbSet<T>()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var dialect = new PostgresSqlDialect(typeMapper);
            var lazyLoader = new LazyLoader(connection, dialect);            
            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator, lazyLoader);
            var dbSet = new DbSet<T>(provider);
            connection.Open();
            return dbSet;
        }
        
        static void Main(string[] args)
        {
            Show.Linq.ShowToList();
        }
    }
}