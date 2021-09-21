using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ORM.Core.Models;

namespace ORM.Infrastructure.Database
{
    public class DbSet<TEntity> where TEntity : class
    {
        private readonly ICollection<TEntity> _entities = new List<TEntity>();
    }

    public static class DbSetExtensions
    {
        public static IEnumerable<TSource> Where<TSource>(
            this DbSet<TSource> set, 
            Func<TSource, bool> predicate) where TSource : class
        {
            var table = new EntityTable(typeof(TSource));
            var columnNames = table.Columns.Select(c => c.Name);
            string columns = string.Join(",", columnNames);
            string sql = $"SELECT {columns} FROM {table.Columns}";
            return new List<TSource>();
        }
    }
}