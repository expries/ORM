using Npgsql;
using ORM.Core;
using ORM.Core.Caching;
using ORM.Core.Loading;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;

namespace ORM.Application
{
    public static class DbFactory
    {
        public const string ConnectionString = "Server=localhost;Port=5434;User Id=postgres;Password=postgres;";

        public static DbContext CreateDbContext(string connectionString = ConnectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var dialect = new PostgresCommandBuilder(connection, typeMapper);
            var cache = new EntityCache();
            var dbContext = new DbContext(dialect, cache);
            connection.Open();
            return dbContext;
        }

        public static DbSet<T> CreateDbSet<T>(string connectionString = ConnectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            var lazyLoader = new LazyLoader(commandBuilder);            
            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator, lazyLoader);
            var dbSet = new DbSet<T>(provider);
            connection.Open();
            return dbSet;
        }
    }
}