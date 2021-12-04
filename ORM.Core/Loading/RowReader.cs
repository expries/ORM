using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Loading
{
    internal class RowReader<T> : IEnumerator<T>
    {
        private readonly IDataReader _reader;
        
        private readonly ILazyLoader _lazyLoader;

        private readonly ICache _cache;

        private Dictionary<int, string>? _schema;

        public T Current { get; private set; }

        object? IEnumerator.Current => Current;
        
        public RowReader(IDataReader reader, ILazyLoader lazyLoader, ICache cache)
        {
            _reader = reader;
            _lazyLoader = lazyLoader;
            _cache = cache;
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
                var proxyType = ProxyFactory.CreateProxy(typeof(T));
                Current = (dynamic) Activator.CreateInstance(proxyType);
                ReadExternalType();  
                _cache.Save(Current);
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

            Console.WriteLine(
                $"Invoking {loadMethodInfo.Name} for {Current.GetType().Name} to " +
                $"resolve {property.Name} ({property.PropertyType.FullName})");
            
            // build lazy loading method
            var loadMethod = loadMethodInfo.MakeGenericMethod(typeof(T), externalType);
            object? lazyResult = loadMethod.Invoke(this, new object[] {Current});

            // get backing field of proxy and write lazy result to it
            var fields = Current.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            var backingField = fields.FirstOrDefault(f => f.Name == $"_lazy{property.Name}");
            backingField?.SetValue(Current, lazyResult);
            Console.WriteLine();
        }

        private Lazy<List<TMany>> LoadOneToMany<TOne, TMany>(TOne entity)
        {
            List<TMany> Loading() => _lazyLoader.LoadOneToMany<TOne, TMany>(entity);
            return new Lazy<List<TMany>>(Loading);
        }

        private Lazy<TOne> LoadManyToOne<TMany, TOne>(TMany entity)
        {
            TOne Loading() => _lazyLoader.LoadManyToOne<TMany, TOne>(entity);
            return new Lazy<TOne>(Loading);
        }
        
        private Lazy<List<TManyB>> LoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            List<TManyB> Loading() => _lazyLoader.LoadManyToMany<TManyA, TManyB>(entity);
            return new Lazy<List<TManyB>>(Loading);
        }
    }
}