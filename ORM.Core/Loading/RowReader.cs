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

namespace ORM.Core.Loading
{
    /// <summary>
    /// Maps result rows from a data reader into objects of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RowReader<T> : IEnumerator<T>
    {
        /// <summary>
        /// A data reader for reading result rows
        /// </summary>
        private readonly IDataReader _reader;
        
        /// <summary>
        /// A loader to fetch entity relationships when encountered
        /// </summary>
        private readonly ILazyLoader _lazyLoader;

        /// <summary>
        /// The schema for the current row
        /// </summary>
        private Dictionary<int, string>? _schema;

        /// <summary>
        /// The mapped object
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Enumerator for the mapped object
        /// </summary>
        object? IEnumerator.Current => Current;
        
        public RowReader(IDataReader reader, ILazyLoader lazyLoader)
        {
            _reader = reader;
            _lazyLoader = lazyLoader;
        }
        
        /// <summary>
        /// Read the next row.
        /// Returns true if a row was read successfully or false if no row is left.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            bool recordWasFound = _reader.Read();

            if (!recordWasFound)
            {
                return false;
            }

            // Read the schema once
            if (_schema is null)
            {
                ReadColumnSchema();
            }

            // Read internal type (e.g. primitive types)
            if (typeof(T).IsInternalType())
            {
                ReadInternalType();
            }
            // Read entities
            else
            {
                // Create a lazy proxy for lazy-loading relationship properties
                Current = LazyProxyFactory.CreateProxy<T>();
                // Fill the object with data
                ReadExternalType();
            }

            return true;
        }

        /// <summary>
        /// Used for resetting the enumerator.
        /// As the enumerator may only be consumed once, this function needs no implementation.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Disposing the enumerator
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        /// <summary>
        /// Read the schema for the current row
        /// </summary>
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

        /// <summary>
        /// Reads an internal type
        /// </summary>
        /// <exception cref="ObjectMappingException"></exception>
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

        /// <summary>
        /// Reads an external type (e.g. an entity)
        /// </summary>
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
        
        /// <summary>
        /// Sets an internal property on the current object
        /// </summary>
        /// <param name="property"></param>
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

        /// <summary>
        /// Sets an external property on the current object
        /// </summary>
        /// <param name="property"></param>
        /// <param name="externalType"></param>
        /// <exception cref="ObjectMappingException"></exception>
        private void SetExternalProperty(PropertyInfo property, Type externalType)
        {
            var table = typeof(T).ToTable();
            var relationship = table.RelationshipTo(externalType);

            // select lazy loader method to use
            var loadMethodInfo = relationship switch
            {
                RelationshipType.OneToOne => GetType().GetMethod(nameof(LoadOneToMany), BindingFlags.Instance | BindingFlags.NonPublic),
                RelationshipType.OneToMany => GetType().GetMethod(nameof(LoadOneToMany), BindingFlags.Instance | BindingFlags.NonPublic),
                RelationshipType.ManyToOne => GetType().GetMethod(nameof(LoadManyToOne), BindingFlags.Instance | BindingFlags.NonPublic),
                RelationshipType.ManyToMany => GetType().GetMethod(nameof(LoadManyToMany), BindingFlags.Instance | BindingFlags.NonPublic),
                _ => throw new ObjectMappingException($"No relation found for property {property.Name} on type {typeof(T).Name}")
            };
            
            // build lazy loading method
            var loadMethod = loadMethodInfo.MakeGenericMethod(typeof(T), externalType);
            object? lazyResult = loadMethod.Invoke(this, new object[] {Current});

            // get backing field of proxy and write lazy result to it
            var fields = Current.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            var backingField = fields.FirstOrDefault(f => f.Name == $"_lazy{property.Name}");
            backingField?.SetValue(Current, lazyResult);
        }

        /// <summary>
        /// Returns a lazy list that executes the loader when fetching its value
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TOne"></typeparam>
        /// <typeparam name="TMany"></typeparam>
        /// <returns></returns>
        private Lazy<List<TMany>> LoadOneToMany<TOne, TMany>(TOne entity)
        {
            List<TMany> Loading() => _lazyLoader.LoadOneToMany<TOne, TMany>(entity);
            return new Lazy<List<TMany>>(Loading);
        }

        /// <summary>
        /// Returns a lazy object that executes the loader when fetching its value
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        private Lazy<TOne> LoadManyToOne<TMany, TOne>(TMany entity)
        {
            TOne Loading() => _lazyLoader.LoadManyToOne<TMany, TOne>(entity);
            return new Lazy<TOne>(Loading);
        }
        
        /// <summary>
        /// Returns an lazy list that executes the loader when fetching its value
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TManyA"></typeparam>
        /// <typeparam name="TManyB"></typeparam>
        /// <returns></returns>
        private Lazy<List<TManyB>> LoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            List<TManyB> Loading() => _lazyLoader.LoadManyToMany<TManyA, TManyB>(entity);
            return new Lazy<List<TManyB>>(Loading);
        }
    }
}