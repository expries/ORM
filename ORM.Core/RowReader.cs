using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core
{
    internal class RowReader<T> : IEnumerator<T>
    {
        private readonly IDataReader _reader;
        
        private readonly ILazyLoader _lazyLoader;

        private Dictionary<int, string>? _schema;

        public T Current { get; private set; }

        object? IEnumerator.Current => Current;
        
        public RowReader(IDataReader reader, ILazyLoader lazyLoader)
        {
            _reader = reader;
            _lazyLoader = lazyLoader;
        }
        
        public bool MoveNext()
        {
            bool recordWasFound = _reader.Read();

            if (!recordWasFound)
            {
                return false;
            }

            if (_schema is null)
            {
                ReadColumnSchema();
            }

            if (typeof(T).IsConvertibleToDbColumn())
            {
                ReadValueType();
                return true;
            }

            Current = Activator.CreateInstance<T>();
            ReadComplexType();
            return true;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        private void ReadColumnSchema()
        {
            _schema = new Dictionary<int, string>();
            int columnsCount = _reader.FieldCount;
            
            for (int i = 0; i < columnsCount; i++)
            {
                string columnName = _reader.GetName(i);
                _schema[i] = columnName;
            }
        }

        private void ReadValueType()
        {
            if (_schema.Count < 1)
            {
                throw new ObjectMappingException("Statement yielded no result to map.");
            }

            var valueType = _reader.GetFieldType(0);
            
            if (valueType == typeof(long) && typeof(T) == typeof(int))
            {
                Current = (T) (object) _reader.GetInt32(0);
                return;
            }
            
            object value = _reader.GetValue(0);
            Current = (T) value;
        }

        private void ReadComplexType()
        {
            var table = typeof(T).ToTable();
            var properties = typeof(T).GetProperties();

            var simpleProperties = properties.Where(p => 
                p.PropertyType.IsConvertibleToDbColumn());

            var complexProperties = properties.Where(p =>
                !p.PropertyType.IsConvertibleToDbColumn());

            foreach (var property in simpleProperties)
            {
                MapColumn(property);
            }
            
            var pkColumn = table.Columns.First(c => c.IsPrimaryKey);
            var pkProperty = properties.First(p => new Column(p).Name == pkColumn.Name);
            object? pk = pkProperty.GetValue(Current);

            foreach (var property in complexProperties)
            {
                var type = property.PropertyType;
                var entityType = type.IsCollectionOfAType() 
                    ? type.GetGenericArguments().First() 
                    : type;

                var entityTable = entityType.ToTable();

                switch (table.RelationshipTo(entityTable))
                {
                    case RelationshipType.OneToOne:
                    case RelationshipType.OneToMany:
                        MapOneToMany(property, entityTable, pk);
                        break;
                    
                    case RelationshipType.ManyToOne:
                        MapManyToOne(property, entityTable, pk);
                        break;
                    
                    case RelationshipType.ManyToMany:
                        MapManyToMany(property, entityTable, pk);
                        break;
                    
                    case RelationshipType.None:
                        throw new ObjectMappingException($"No relation found for property {property.Name} " +
                                                         $"on type {typeof(T).Name}");
                }
            }
        }
        
        private void MapColumn(PropertyInfo property)
        {
            var column = new Column(property);
            bool schemaContainsColumn = _schema.Values.Any(s => s.ToLower() == column.Name.ToLower());
            
            if (!schemaContainsColumn)
            {
                property.SetValue(Current, null);
                return;
            }
            
            var columnSchema = _schema.FirstOrDefault(
                kv => kv.Value.ToLower() == column.Name.ToLower());

            int columnOrdinal = columnSchema.Key;
            object value = _reader.GetValue(columnOrdinal);

            if (value is DBNull)
            {
                property.SetValue(Current, null);
                return;
            }
            
            property.SetValue(Current, value);
        }

        private void MapOneToMany(PropertyInfo property, EntityTable entityTable, object pk)
        {
            var method = typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadOneToMany));
            var genericMethod = method.MakeGenericMethod(typeof(T), entityTable.Type);
            var result = genericMethod.Invoke(_lazyLoader, new[] { pk });
            property.SetValue(Current, result);
        }
        
        private void MapManyToOne(PropertyInfo property, EntityTable entityTable, object pk)
        {
            var method = typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadManyToOne));
            var genericMethod = method.MakeGenericMethod(typeof(T), entityTable.Type);
            var result = genericMethod.Invoke(_lazyLoader, new[] { pk });
            property.SetValue(Current, result);
        }
        
        private void MapManyToMany(PropertyInfo property, EntityTable entityTable, object pk)
        {
            var method = typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadManyToMany));
            var genericMethod = method.MakeGenericMethod(typeof(T), entityTable.Type);
            var result = genericMethod.Invoke(_lazyLoader, new[] { pk });
            property.SetValue(Current, result);
        }
    }
}