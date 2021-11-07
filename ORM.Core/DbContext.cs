using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ORM.Core.Interfaces;
using ORM.Core.Models;

namespace ORM.Core
{
    public class DbContext : IDbContext
    {
        private readonly ISqlDialect _sqlDialect;
        
        private readonly IDbConnection _connection;

        public DbContext(ISqlDialect dialect, IDbConnection connection)
        {
            _sqlDialect = dialect;
            _connection = connection;
        }
        
        public void EnsureCreated(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            var tables = GetTables(assembly).ToList();
            string dropTablesSql = _sqlDialect.TranslateDropTables(tables);
            string createTablesSql = _sqlDialect.TranslateCreateTables(tables);
            string addForeignKeysSql = _sqlDialect.TranslateAddForeignKeys(tables);

            var cmd = _connection.CreateCommand();
            cmd.CommandText = dropTablesSql + createTablesSql + addForeignKeysSql;
            cmd.ExecuteNonQuery();
        }

        public void Save<T>(T entity)
        {
            var table = new EntityTable(typeof(T));
            string sql = _sqlDialect.TranslateInsert(table, entity);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<T> GetAll<T>()
        {
            var entityTable = new EntityTable(typeof(T));
            string sql  = _sqlDialect.TranslateSelect(entityTable);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            
            return new ObjectReader<T>(reader);
        }

        public T GetById<T>(object pk)
        {
            var entityTable = new EntityTable(typeof(T));
            string sql  = _sqlDialect.TranslateSelectById(entityTable, pk);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            
            return (T) new ObjectReader<T>(reader);
        }

        private IEnumerable<Table> GetTables(Assembly assembly)
        {
            var entityTypes = GetEntityTypes(assembly);
            var entityTables = entityTypes.Select(t => new EntityTable(t));
            var tables = GetAllTables(entityTables);
            return tables;
        }
        
        private static IEnumerable<Table> GetAllTables(IEnumerable<Table> tables)
        {
            var tableList = tables.ToList();
            var allTables = tableList;
            
            foreach (var table in tableList)
            {
                var relatedTables = table.Relationships.Select(r => r.Table);
                allTables = allTables.Union(relatedTables).ToList();
            }

            return allTables;
        }
        
        private IEnumerable<Type> GetEntityTypes(Assembly assembly)
        {
            var dbContexts = GetDbContextTypes(assembly);
            var entities = new List<Type>();
            
            foreach (var context in dbContexts)
            {
                var properties = context.GetProperties();
                var propertyTypes = properties.Select(p => p.PropertyType).Distinct();
                var genericProperties = propertyTypes.Where(t => t.IsGenericType);
                var dbSetProperties = genericProperties.Where(t => t.GetGenericTypeDefinition() == typeof(DbSet<>));
                var entityTypes = dbSetProperties.Select(t => t.GetGenericArguments().FirstOrDefault());
                entities = entities.Union(entityTypes).ToList();
            }

            return entities;
        }
        
        private IEnumerable<Type> GetDbContextTypes(Assembly assembly)
        {
            var dbContextType = GetType();
            var types = assembly.GetTypes();
            var contexts = types.Where(t => t.IsSubclassOf(dbContextType));
            return contexts;
        }
    }
}