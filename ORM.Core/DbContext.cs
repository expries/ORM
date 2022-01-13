using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ORM.Core.Caching;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Extensions;

namespace ORM.Core
{
    /// <summary>
    /// Database context for performing methods in the current database context
    /// </summary>
    public class DbContext : IDbContext
    {
        /// <summary>
        /// Builds commands to be executed
        /// </summary>
        private readonly ICommandBuilder _commandBuilder;

        /// <summary>
        /// Caches entities
        /// </summary>
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
        
        /// <summary>
        /// Creates the database schema
        /// </summary>
        /// <param name="assembly"></param>
        public void EnsureCreated(Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var tables = GetTables(assembly).ToList();
            var cmd = _commandBuilder.BuildEnsureCreated(tables);
            cmd.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Saves an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        public void Save<T>(T entity)
        {
            // Do nothing if cached entity did not changed
            if (!_cache.HasChanged(entity))
            {
                return;
            }

            // Update references on object
            UpdateReferences(entity);

            // Save entity
            SaveEntity(entity);
            
            // Save many to many references
            SaveReferences(entity);
        }
        
        /// <summary>
        /// Saves an entity as-is and updates its primary key
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private object SaveEntity<T>(T entity)
        {
            // Save entity
            var saveCmd = _commandBuilder.BuildSave(entity);
            object? pk = saveCmd.ExecuteScalar();
                    
            // Update primary key on entity
            var type = entity.GetType();
            var entityTable = type.ToTable();
            entityTable.PrimaryKey.SetValue(entity, pk);
            return pk;
        }

        /// <summary>
        /// Updates references of an entity
        /// </summary>
        /// <param name="entity"></param>
        private static void UpdateReferences(object entity)
        {
            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();
            var references = entityTable.GetPropertiesOf(RelationshipType.OneToMany);
            
            foreach (var property in references)
            {
                var navigatedCollection = property.GetValue(entity) as IEnumerable<object> ?? new List<object>();

                foreach (var item in navigatedCollection)
                {
                    var itemType = item.GetType();
                    var navigatedProperty = itemType.GetNavigatedProperty(entityType);
                    navigatedProperty?.SetValue(item, entity);
                }
            }
        }

        /// <summary>
        /// Saves the references of an entity in the database
        /// </summary>
        /// <param name="entity"></param>
        private void SaveReferences(object entity)
        {
            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();
            var references = entityTable.GetPropertiesOf(RelationshipType.ManyToMany);
            
            // save many to many references
            foreach (var property in references)
            {
                var entries = property.GetValue(entity) as IEnumerable<object> ?? new List<object>();
                entries = entries.ToList();
                
                var referencesPrimaryKeys = entries.Select(SaveEntity).ToList();
                var referenceType = property.PropertyType.GetUnderlyingType();
                
                var removeCmd = _commandBuilder.BuildRemoveManyToManyReferences(entity, referenceType);
                removeCmd.ExecuteNonQuery();

                if (entries.ToList().Count > 0)
                {
                    var addCmd = _commandBuilder.BuildSaveManyToManyReferences(entity, referenceType, referencesPrimaryKeys);
                    addCmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Get all entities of a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>()
        {
            var cmd = _commandBuilder.BuildGetAll<T>();
            var reader = cmd.ExecuteReader();
            return CreateObjectReader<T>(reader);
        }

        /// <summary>
        /// Get an entity by its primary key
        /// </summary>
        /// <param name="pk"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetById<T>(object pk)
        {
            var cmd = _commandBuilder.BuildGetById<T>(pk);
            var reader = cmd.ExecuteReader();
            return (T) CreateObjectReader<T>(reader);
        }

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        /// <param name="pk"></param>
        public void DeleteById<T>(object pk)
        {
            var cmd = _commandBuilder.BuildDeleteById<T>(pk);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Create an object reader to create objects usign a data reader
        /// </summary>
        /// <param name="dataReader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private ObjectReader<T> CreateObjectReader<T>(IDataReader dataReader)
        {
            var loader = new LazyLoader(_commandBuilder);
            return new ObjectReader<T>(dataReader, loader);            
        }

        /// <summary>
        /// Get the tables for all entities in a given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private IEnumerable<Table> GetTables(Assembly assembly)
        {
            var entityTypes = GetEntityTypes(assembly);
            var entityTables = entityTypes.Select(t => t.ToTable());
            var tables = GetAllTables(entityTables);
            return tables;
        }
        
        /// <summary>
        /// Returns an union of the given tables and all connected tables
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
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
        
        /// <summary>
        /// Returns all entity types in a given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
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
        
        /// <summary>
        /// Gets all the types that inherit from DbContext in a given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private IEnumerable<Type> GetDbContextTypes(Assembly assembly)
        {
            var dbContextType = GetType();
            var types = assembly.GetTypes();
            var contexts = types.Where(t => t.IsSubclassOf(dbContextType));
            return contexts;
        }
    }
}