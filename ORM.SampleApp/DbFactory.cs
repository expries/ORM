using Npgsql;
using ORM.Application.DbContexts;
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
        private const string ConnectionString = "Server=localhost;Port=5434;User Id=postgres;Password=postgres;";

        public static ShopContext CreateDbContext(string connectionString = ConnectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            var trackingCache = new StateTrackingCache();
            var dbContext = new ShopContext(commandBuilder, trackingCache);
            connection.Open();
            return dbContext;
        }

        public static DbSet<T> CreateDbSet<T>(string connectionString = ConnectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            var typeMapper = new PostgresDataTypeMapper();
            
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            var linqCommandBuilder = new LinqCommandBuilder(connection);
            
            var lazyLoader = new LazyLoader(commandBuilder);            
            var provider = new QueryProvider(linqCommandBuilder, lazyLoader);
            var dbSet = new DbSet<T>(provider);
            
            connection.Open();
            return dbSet;
        }
    }
}