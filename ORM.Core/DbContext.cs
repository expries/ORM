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
            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();

            var oneToMany = entityTable.GetPropertiesOf(RelationshipType.OneToMany);
            var manyToMany = entityTable.GetPropertiesOf(RelationshipType.ManyToMany);

            // update one to many references
            foreach (var property in oneToMany)
            {
                var navigatedCollection = property.GetValue(entity) as IEnumerable<object> ?? new List<object>();

                foreach (var item in navigatedCollection)
                {
                    var itemType = item.GetType();
                    var navigatedProperty = itemType.GetNavigatedProperty(entityType);
                    navigatedProperty?.SetValue(item, entity);
                }
            }
            
            // save entity
            var cmd = _commandBuilder.BuildSave(entity);
            object? pk = cmd.ExecuteScalar();
            entityTable.PrimaryKey.SetValue(entity, pk);

            // save many to many references
            foreach (var property in manyToMany)
            {
                var references = property.GetValue(entity) as IEnumerable<object> ?? new List<object>();
                var referencePrimaryKeys = new List<object?>();
                
                foreach (var reference in references)
                {
                    // save reference
                    var referencePk = SaveEntity(reference);
                    referencePrimaryKeys.Add(referencePk);
                }

                var referenceType = property.PropertyType.GetUnderlyingType();
                UpdateManyToManyTable(entityType, referenceType, pk, referencePrimaryKeys);
            }
        }

        private object SaveEntity<T>(T entity)
        {
            // save reference
            var saveCmd = _commandBuilder.BuildSave(entity);
            object? pk = saveCmd.ExecuteScalar();
                    
            // update primary key of reference
            var type = entity.GetType();
            var entityTable = type.ToTable();
            entityTable.PrimaryKey.SetValue(entity, pk);
            return pk;
        } 

        private void UpdateManyToManyTable(Type source, Type dest, object sourcePk, List<object?> destinationPks)
        {
            var sourceTable = source.ToTable();
            var destTable = dest.ToTable();
            
            // TODO: Move foreign key table detection to other class (maybe table?)

            // get foreign key helper table
            var fkTable = sourceTable.ForeignKeyTables.First(x =>
                x.TableA.Type == sourceTable.Type &&
                x.TableB.Type == destTable.Type ||
                x.TableA.Type == destTable.Type &&
                x.TableB.Type == sourceTable.Type
            );
            
            // get foreign key table columns
            var sourceColumn = fkTable.ForeignKeys
                .First(fk => fk.TableTo.Name == sourceTable.Name)
                .ColumnFrom;
            
            var destColumn = fkTable.ForeignKeys
                .First(fk => fk.TableTo.Name == destTable.Name)
                .ColumnFrom;
            
            // TODO: Delete prior entries in table

            var subCmd = _commandBuilder.Connection.CreateCommand();
            subCmd.Parameters.AddWithValue("pk", sourcePk);

            subCmd.CommandText = $"INSERT INTO \"{fkTable.Name}\" (\"{sourceColumn.Name}\", \"{destColumn.Name}\") " +
                                 $"{Environment.NewLine}";

            for (int i = 0; i < destinationPks.Count; i++)
            {
                object destinationPk = destinationPks[i];
                subCmd.CommandText += $"VALUES (@pk, @pkDest{i})";

                if (i < destinationPks.Count - 1)
                {
                    subCmd.CommandText += ",";
                }

                subCmd.CommandText += Environment.NewLine;
                subCmd.Parameters.AddWithValue($"pkDest{i}", destinationPk);
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