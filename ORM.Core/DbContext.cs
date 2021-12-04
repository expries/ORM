using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ORM.Core.Cache;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Models;
using ORM.Core.Models.Extensions;

namespace ORM.Core
{
    public class DbContext : IDbContext
    {
        private readonly ICommandBuilder _commandBuilder;

        private readonly ICache _cache;

        public DbContext(ICommandBuilder dialect, ICache cache)
        {
            _commandBuilder = dialect;
            _cache = cache;
        }

        protected DbContext(ICommandBuilder dialect) 
            : this(dialect, new EntityCache())
        {
        }

        public void EnsureCreated(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var tables = GetTables(assembly).ToList();
            var cmd = _commandBuilder.BuildEnsureCreated(tables);
            cmd.ExecuteNonQuery();
        }

        public void Save<T>(T entity)
        {
            var cmd = _commandBuilder.BuildSave(entity);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<T> GetAll<T>()
        {
            var cmd = _commandBuilder.BuildGetAll<T>();
            var reader = cmd.ExecuteReader();
            return CreateObjectReader<T>(reader);
        }

        public T GetById<T>(object pk)
        {
            var cmd = _commandBuilder.BuildGetById<T>(pk);
            var reader = cmd.ExecuteReader();
            return (T) CreateObjectReader<T>(reader);
        }

        private ObjectReader<T> CreateObjectReader<T>(IDataReader dataReader)
        {
            var loader = new LazyLoader(_commandBuilder, _cache);
            return new ObjectReader<T>(dataReader, loader, _cache);            
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
            IEnumerable<Table> allTables = tableList;
            
            foreach (var table in tableList)
            {
                var externalTables = table.ExternalFields.Select(_ => _.Table);
                var manyToManyTables = table.ForeignKeyTables;
                allTables = allTables.Union(externalTables).ToList();
                allTables = allTables.Union(manyToManyTables).ToList();
            }
            
            return allTables.ToList();
        }
        
        private List<Type> GetEntityTypes(Assembly assembly)
        {
            var dbContexts = GetDbContextTypes(assembly);
            var entities = new List<Type>();
            
            foreach (var context in dbContexts)
            {
                var properties = context.GetProperties();
                var propertyTypes = properties.Select(p => p.PropertyType).Distinct();
                var genericProperties = propertyTypes.Where(t => t.IsGenericType);
                var dbSetProperties = genericProperties.Where(t => t.GetGenericTypeDefinition() == typeof(DbSet<>));
                var entityTypes = dbSetProperties.Select(t => t.GetGenericArguments().First());
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