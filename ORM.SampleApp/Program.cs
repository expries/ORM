using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Npgsql;
using ORM.Application.Entities;
using ORM.Core;
using ORM.Core.Cache;
using ORM.Core.Loading;
using ORM.Linq;
using ORM.Postgres.Linq;
using ORM.Postgres.SqlDialect;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Permissions;
using ORM.Core.Models.Exceptions;

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
            var cache = new EntityCache();
            var dbContext = new DbContext(dialect, cache);
            connection.Open();
            return dbContext;
        }

        public static DbSet<T> CreateDbSet<T>()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            var typeMapper = new PostgresDataTypeMapper();
            var commandBuilder = new PostgresCommandBuilder(connection, typeMapper);
            var cache = new EntityCache();
            var lazyLoader = new LazyLoader(commandBuilder, cache);            
            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator, lazyLoader);
            var dbSet = new DbSet<T>(provider);
            connection.Open();
            return dbSet;
        }

        static void Main(string[] args)
        {
            var author = (object) new Author();
            var result = (dynamic) Convert.ChangeType(author, typeof(Author));
            var ef =  new Lazy<Author>(() => result);

            var ctx = CreateDbContext();
            //ctx.EnsureCreated();
            //Show.SaveObject.ShowAuthor();
            
            
            Show.SaveObject.ShowAuthor();
            Show.SaveObject.ShowBook();
            Show.SaveObject.ShowSeller();
            
            var books = ctx.GetAll<Book>().ToList();
            var authors = ctx.GetAll<Author>().ToList();
            var sellers = ctx.GetAll<Seller>().ToList();
            
            
            //Show.SaveObject.ShowProduct();
            //var authors = ctx.GetAll<Author>().ToList();
            Console.WriteLine();
            //Show.GetEntity.GetAuthor();
            //Show.SaveObject.ShowAuthor()oList();
        }
    }
}