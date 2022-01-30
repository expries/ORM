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
        private Dictionary<string, int> _schema;

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

            // Read the schema if it hasn't been read yet
            if (_schema is null)
            {
                ReadColumnSchema();
            }
            
            // Read value type
            if (typeof(T).IsValueType())
            {
                ReadValueType();
            }
            // Read complex type (e.g. entity)
            else
            {
                // Create a lazy proxy for lazy-loading relationship properties
                Current = LazyProxyFactory.CreateProxy<T>();
                ReadComplexType();
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
            _schema = new Dictionary<string, int>();
            int columnsCount = _reader.FieldCount;
            
            for (int i = 0; i < columnsCount; i++)
            {
                string columnName = _reader.GetName(i);
                _schema[columnName] = i;
            }
        }

        /// <summary>
        /// Reads a value type
        /// </summary>
        /// <exception cref="ObjectMappingException"></exception>
        private void ReadValueType()
        {
            if (_schema.Count < 1)
            {
                throw new ObjectMappingException("Statement yielded no result to map.");
            }

            var valueType = _reader.GetFieldType(0);
            object? value;

            // Read as integer if property is int and database column is long
            if (valueType == typeof(long) && 
                typeof(T) == typeof(int) && 
                !_reader.IsDBNull(0))
            {
                value = _reader.GetInt32(0);
            } 
            // Read value
            else
            {
                value = _reader.GetValue(0);
            }

            // If result is integer or long (like in LINQ's Count()-Method, then convert NULL to 0)
            if (valueType == typeof(int) || valueType == typeof(long))
            {
                value = value == DBNull.Value ? 0 : value;
            }

            Current = (T) value;
        }

        /// <summary>
        /// Reads an external type (e.g. an entity)
        /// </summary>
        private void ReadComplexType()
        {
            var properties = typeof(T).GetProperties();

            foreach (var property in properties.Where(p => p.PropertyType.IsValueType()))
            {
                SetInternalProperty(property);
            }

            foreach (var property in properties.Where(p => !p.PropertyType.IsValueType()))
            {
                var entityType = property.PropertyType.GetUnderlyingType();
                SetExternalProperty(property, entityType);
            }
        }
        
        /// <summary>
        /// Sets an internal property on the current object
        /// </summary>
        /// <param name="property"></param>
        private void SetInternalProperty(PropertyInfo property)
        {
            var column = new Column(property);
            object? value = ReadValue(column);
            property.SetValue(Current, value);
        }

        /// <summary>
        /// Read value of column from row
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private object? ReadValue(Column column)
        {
            if (!_schema.ContainsKey(column.Name))
            {
                return null;
            }

            int index = _schema[column.Name];
            return _reader.GetValue(index);
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

            // Select loader method to use
            var type = GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var loadMethodInfo = relationship switch
            {
                RelationshipType.OneToOne   => type.GetMethod(nameof(LoadOneToMany), flags),
                RelationshipType.OneToMany  => type.GetMethod(nameof(LoadOneToMany), flags),
                RelationshipType.ManyToOne  => type.GetMethod(nameof(LoadManyToOne), flags),
                RelationshipType.ManyToMany => type.GetMethod(nameof(LoadManyToMany), flags),
                _ => throw new ObjectMappingException($"No relation found for property {property.Name} on type {typeof(T).Name}")
            };

            if (loadMethodInfo is null)
            {
                throw new ObjectMappingException($"Failed to find load method for relationship {relationship}");
            }

            // Build loading method
            var loadMethod = loadMethodInfo.MakeGenericMethod(typeof(T), externalType);
            object? lazyResult = loadMethod.Invoke(this, new object[] { Current });

            // Get lazy backing field of proxy and write lazy result to it
            var fields = Current?.GetType().GetFields(flags);
            var backingField = fields?.FirstOrDefault(f => f.Name == $"_lazy{property.Name}");
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