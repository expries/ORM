using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ORM.Core.Interfaces;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Caching
{
    public class StateTrackingCache : ICache
    {
        private readonly Dictionary<(Type, object), object> _cache = new Dictionary<(Type, object), object>();
        
        private readonly Dictionary<(Type, object), string> _hashes = new Dictionary<(Type, object), string>();

        /// <summary>
        /// Saves an entity to the cache
        /// </summary>
        /// <param name="entity"></param>
        public void Save(object entity)
        {
            var table = entity.GetType().ToTable();
            object? pk = table.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                throw new OrmException("Primary key is not set on object.");
            }
            
            _cache[(table.Type, pk)] = entity;
            _hashes[(table.Type, pk)] = ComputeHash(entity);
        }

        /// <summary>
        /// Removes an entity from the cache.
        /// Returns successfully whether entity was present in cache or not
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(object entity)
        {
            var table = entity.GetType().ToTable();
            object? pk = table.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                return;
            }
            
            if (_cache.ContainsKey((table.Type, pk)))
            {
                _cache.Remove((table.Type, pk));
            }
        }

        /// <summary>
        /// Gets all the entities of a given type from the cache
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<object> GetAll(Type type)
        {
            var table = type.ToTable();
            
            var entities = _cache
                .Where(kv => kv.Key.Item1 == table.Type)
                .Select(kv => kv.Value)
                .ToList();
            
            return entities;
        }

        /// <summary>
        /// Gets an entity of a given type that has the provided primary key from the cache
        /// </summary>
        /// <param name="type"></param>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public object? Get(Type type, object? primaryKey)
        {
            if (primaryKey is null)
            {
                return null;
            }

            var table = type.ToTable();
            
            if (_cache.ContainsKey((table.Type, primaryKey)))
            {
                return _cache[(table.Type, primaryKey)];
            }

            return null;
        }
        
        /// <summary>
        /// Returns whether a given entity is different to its cached version.
        /// Returns true if the entity was not found in the cache or if it is null.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasChanged(object? entity)
        {
            if (entity is null)
            {
                return true;
            }
            
            var table = entity.GetType().ToTable();
            object? pk = table.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                return true;
            }

            string? storedHash = GetHash(entity, pk);
            string computedHash = ComputeHash(entity);
            
            return storedHash is null || storedHash != computedHash;
        }

        /// <summary>
        /// Get stored hash of an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private string? GetHash(object entity, object pk)
        {
            var type = entity.GetType();
            return _hashes.ContainsKey((type, pk)) ? _hashes[(type, pk)] : null;
        }

        /// <summary>
        /// Compute hash for an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private static string ComputeHash(object entity)
        {
            var type = entity.GetType();
            var table = type.ToTable();
            string hashString = string.Empty;

            // internal fields
            foreach (var column in table.Columns)
            {
                object? value = column.GetValue(entity);

                if (value is null)
                {
                    continue;
                }

                if (column.IsForeignKey)
                {
                    var pk = column.GetValue(entity);
                    hashString += pk;
                }
                else
                {
                    hashString += $"{column.Name}={value};";
                }
            }

            // external fields
            foreach (var property in table.Type.GetProperties())
            {
                var propertyType = property.PropertyType;
                object? value = property.GetValue(entity);

                if (propertyType.IsValueType() || value is null)
                {
                    continue;
                }
                
                var entityType = propertyType.GetUnderlyingType();
                var entityTable = entityType.ToTable();
                
                // get primary keys of reference collection
                if (propertyType.IsCollectionOfOneType())
                {
                    var collection = value as IEnumerable ?? new List<object>();
                    
                    foreach (object? x in collection)
                    {
                        object? xpk = entityTable.PrimaryKey.GetValue(x);
                        hashString += $"{xpk},";
                    }

                    continue;
                }
                
                // get primary of single reference
                object? pk = entityTable.PrimaryKey.GetValue(value);

                if (pk is not null)
                {
                    hashString += $"{pk}";
                }
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(hashString);
            byte[] hashBytes = SHA256.Create().ComputeHash(utf8Bytes);
            string hashUtf8String = Encoding.UTF8.GetString(hashBytes);
            return hashUtf8String;
        }
    }
}