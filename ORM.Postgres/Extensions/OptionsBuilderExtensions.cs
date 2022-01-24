using Npgsql;
using ORM.Core.Configuration;
using ORM.Core.Loading;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;

namespace ORM.Postgres.Extensions
{
    public static class OptionsBuilderExtensions
    {
        public static void UsePostgres(this OptionsBuilder options, string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            
            var lazyLoader = new LazyLoader(commandBuilder);
            var linqCommandBuilder = new LinqCommandBuilder(connection);
            var provider = new QueryProvider(linqCommandBuilder, lazyLoader);
            
            connection.Open();

            options.UseCommandBuilder(commandBuilder);
            options.UseQueryProvider(provider);
        }
    }
}