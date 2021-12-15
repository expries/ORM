using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Npgsql;
using NuGet.Frameworks;
using ORM.Core.Cache;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
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
            // insert/update entity
            var cmd = _commandBuilder.BuildSave(entity);
            var pk = cmd.ExecuteScalar();

            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();
            var manyToMany = entityTable.GetPropertiesOf(RelationshipType.ManyToMany);
            var oneToMany = entityTable.GetPropertiesOf(RelationshipType.OneToMany);

            // update one to many references
            foreach (var oneToManyProperty in oneToMany)
            {
                var collection = oneToManyProperty.GetValue(entity) as IEnumerable<object>;

                foreach (var item in collection)
                {
                    var itemType = item.GetType();
                    var navigatedProperty = itemType.GetNavigatedProperty(entityType);
                    navigatedProperty.SetValue(item, entity);
                    var itemTable = itemType.ToTable();
                    var saveItem = _commandBuilder.BuildSave(item);
                    object? itemPk = saveItem.ExecuteScalar();
                    var pkProperty = itemTable.PrimaryKey.GetProperty(item);
                    pkProperty.SetValue(item, itemPk);
                }
            }
            
            // update many to many references
            foreach (var manyToManyProperty in manyToMany)
            {
                UpdateReference(entity, pk, manyToManyProperty);
            }
        }

        private void UpdateReference<T>(T entity, object entityPk, PropertyInfo manyToManyProperty)
        {
            var entityTable = typeof(T).ToTable();
            var externalType = manyToManyProperty.PropertyType.GetUnderlyingType();
            var externalTable = externalType.ToTable();
            
            // get foreign key helper table
            var fkTable = entityTable.ForeignKeyTables.First(x =>
                x.TableA.Type == entityTable.Type &&
                x.TableB.Type == externalTable.Type ||
                x.TableA.Type == externalTable.Type &&
                x.TableB.Type == entityTable.Type
            );
            
            // get foreign key table columns
            var fkEntity = fkTable.ForeignKeys
                .First(fk => fk.TableTo.Name == entityTable.Name)
                .ColumnFrom;
            
            var fkExternalEntity = fkTable.ForeignKeys
                .First(fk => fk.TableTo.Name == externalTable.Name)
                .ColumnFrom;
            
            // get the primary keys for the collection
            var externalEntities = manyToManyProperty.GetValue(entity) as IEnumerable<dynamic>;
            var externalPrimaryKeys = new List<object>();

            foreach (var externalEntity in externalEntities)
            {
                var insertCmd = _commandBuilder.BuildSave(externalEntity);
                var externalPk = insertCmd.ExecuteScalar();
                externalPrimaryKeys.Add(externalPk);
            }

            var subCmd = _commandBuilder.Connection.CreateCommand();
            subCmd.Parameters.AddWithValue("pk", entityPk);

            subCmd.CommandText = $"INSERT INTO \"{fkTable.Name}\" (\"{fkEntity.Name}\", \"{fkExternalEntity.Name}\") " +
                                 $"{Environment.NewLine}";

            for (int i = 0; i < externalPrimaryKeys.Count; i++)
            {
                object fkPk = externalPrimaryKeys[i];
                subCmd.CommandText += $"VALUES (@pk, @pkFk{i})";

                if (i < externalPrimaryKeys.Count - 1)
                {
                    subCmd.CommandText += ",";
                }

                subCmd.CommandText += Environment.NewLine;
                subCmd.Parameters.AddWithValue($"pkFk{i}", fkPk);
            }
            
            subCmd.ExecuteNonQuery();
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