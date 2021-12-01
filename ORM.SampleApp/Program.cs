using System;
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
        private const string ConnectionString = "Server=localhost;Port=5434;User Id=postgres;Password=postgres;";
        
        public static DbContext CreateDbContext()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var dialect = new PostgresCommandBuilder(connection, typeMapper);
            var dbContext = new DbContext(connection, dialect);
            connection.Open();
            return dbContext;
        }

        public static DbSet<T> CreateDbSet<T>()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            var lazyLoader = new LazyLoader(commandBuilder);            
            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator, lazyLoader);
            var dbSet = new DbSet<T>(provider);
            connection.Open();
            return dbSet;
        }
        
        static void Main(string[] args)
        {
            var ctx = CreateDbContext();
            //ctx.EnsureCreated();
            //Show.GetEntity.GetAuthor();
            //Show.SaveObject.ShowBook();
            var books = ctx.GetAll<Author>().ToList();
            Console.WriteLine();
        }
    }
}