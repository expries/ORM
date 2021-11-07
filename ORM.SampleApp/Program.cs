﻿using System.Linq;
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
            var orm = new DbContext(dialect, connection);
            
            orm.EnsureCreated();
            
            
            var translator = new PostgresQueryTranslator();
            var provider = new QueryProvider(connection, translator);
            var dbSet = new DbSet<Book>(provider);

            var book = new Book();
            book.Title = "My Book :)";
            orm.Save(book);
        }
    }
}