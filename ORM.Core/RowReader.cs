using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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

            if (typeof(T).IsInternalType())
            {
                ReadInternalType();
            }
            else
            {
                Current = Activator.CreateInstance<T>();
                ReadExternalType();  
            }

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

        private void ReadInternalType()
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

        private void ReadExternalType()
        {
            var properties = typeof(T).GetProperties();

            var internalProperties = properties.Where(p => 
                p.PropertyType.IsInternalType());

            var externalProperties = properties.Where(p =>
                !p.PropertyType.IsInternalType());

            foreach (var property in internalProperties)
            {
                SetInternalProperty(property);
            }

            foreach (var property in externalProperties)
            {
                var type = property.PropertyType;
                var entityType = type.IsCollectionOfOneType() 
                    ? type.GetGenericArguments().First() 
                    : type;
                
                SetExternalProperty(property, entityType);
            }
        }
        
        private void SetInternalProperty(PropertyInfo property)
        {
            // check if column is in result schema
            var column = new Column(property);
            bool schemaContainsColumn = _schema.Values.Any(s => s.ToLower() == column.Name.ToLower());
            
            if (!schemaContainsColumn)
            {
                property.SetValue(Current, null);
                return;
            }
            
            var columnSchema = _schema.FirstOrDefault(
                kv => kv.Value.ToLower() == column.Name.ToLower());

            // read value from results
            int columnOrdinal = columnSchema.Key;
            object value = _reader.GetValue(columnOrdinal);

            // set property to value
            if (value is DBNull)
            {
                property.SetValue(Current, null);
                return;
            }
            
            property.SetValue(Current, value);
        }

        private void SetExternalProperty(PropertyInfo property, Type externalType)
        {
            object? pk = GetPrimaryKey();
            var table = typeof(T).ToTable();
            var relationship = table.RelationshipTo(externalType);

            // select lazy loader method to use
            var method = relationship switch
            {
                RelationshipType.OneToOne => typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadOneToMany)),
                RelationshipType.OneToMany => typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadOneToMany)),
                RelationshipType.ManyToOne => typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadManyToOne)),
                RelationshipType.ManyToMany => typeof(ILazyLoader).GetMethod(nameof(LazyLoader.LoadManyToMany)),
                _ => throw new ObjectMappingException($"No relation found for property {property.Name} on type {typeof(T).Name}")
            };
            
            // execute loader method
            var genericMethod = method?.MakeGenericMethod(typeof(T), externalType);
            object? result = genericMethod?.Invoke(_lazyLoader, new[] { pk });
            property.SetValue(Current, result);
        }

        private object? GetPrimaryKey()
        {
            var table = typeof(T).ToTable();
            var properties = typeof(T).GetProperties();
            var pkColumn = table.Columns.First(c => c.IsPrimaryKey);
            var pkProperty = properties.First(p => new Column(p).Name == pkColumn.Name);
            return pkProperty.GetValue(Current);
        }
    }
}