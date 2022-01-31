using System.Diagnostics.CodeAnalysis;
using Npgsql;
using ORM.Core.Configuration;
using ORM.Core.Loading;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;

namespace ORM.Postgres.Extensions
{
    /// <summary>
    /// Extends the options builder for configuring a database context.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class OptionsBuilderExtensions
    {
        /// <summary>
        /// Configure the database context to use a postgres database.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
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