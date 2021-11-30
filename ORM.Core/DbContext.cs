using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Extensions;

namespace ORM.Core
{
    public class DbContext : IDbContext
    {
        private readonly IDbConnection _connection;

        private readonly ICommandBuilder _commandBuilder;

        public DbContext(IDbConnection connection, ICommandBuilder dialect)
        {
            _connection = connection;
            _commandBuilder = dialect;
        }

        public void EnsureCreated(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            var tables = GetTables(assembly).ToList();
            string dropTablesSql = _commandBuilder.TranslateDropTables(tables);
            string createTablesSql = _commandBuilder.TranslateCreateTables(tables);
            string addForeignKeysSql = _commandBuilder.TranslateAddForeignKeys(tables);

            var cmd = _connection.CreateCommand();
            cmd.CommandText = dropTablesSql + createTablesSql + addForeignKeysSql;
            cmd.ExecuteNonQuery();
        }

        public void Save<T>(T entity)
        {
            var table = typeof(T).ToTable();
            string sql = _commandBuilder.TranslateInsert(table, entity);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<T> GetAll<T>()
        {
            var entityTable = typeof(T).ToTable();
            var cmd = _commandBuilder.BuildSelect(entityTable);
            var reader = cmd.ExecuteReader();
            return CreateObjectReader<T>(reader);
        }

        public T GetById<T>(object pk)
        {
            var entityTable = typeof(T).ToTable();
            var cmd = _commandBuilder.BuildSelectById(entityTable, pk);
            var reader = cmd.ExecuteReader();
            
            return (T) CreateObjectReader<T>(reader);
        }

        private ObjectReader<T> CreateObjectReader<T>(IDataReader dataReader)
        {
            var loader = new LazyLoader(_connection, _commandBuilder);
            return new ObjectReader<T>(dataReader, loader);            
        }

        private IEnumerable<Table> GetTables(Assembly assembly)
        {
            var entityTypes = GetEntityTypes(assembly);
            var entityTables = entityTypes.Select(t => t.ToTable());
            var tables = GetAllTables(entityTables);
            return tables;
        }
        
        private static IEnumerable<Table> GetAllTables(IEnumerable<Table> tables)
        {
            var tableList = tables.ToList();
            var allTables = tableList;
            
            foreach (var table in tableList)
            {
                var externalTables = table.ExternalFields.Select(_ => _.Table);
                var manyToManyTables = table.ForeignKeyTables;
                var parentTables = table.ForeignKeys.Where(k => k.IsInheritanceKey).Select(r => r.TableTo);
                allTables = allTables.Union(parentTables).ToList();
                allTables = allTables.Union(externalTables).ToList();
                allTables = allTables.Union(manyToManyTables).ToList();
            }

            foreach (var table in allTables.ToList())
            {
                var parentTables = table.ForeignKeys.Where(k => k.IsInheritanceKey).Select(r => r.TableTo);
                allTables = allTables.Union(parentTables).ToList();
            }

            return allTables;
        }
        
        private List<Type?> GetEntityTypes(Assembly assembly)
        {
            var dbContexts = GetDbContextTypes(assembly);
            var entities = new List<Type?>();
            
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