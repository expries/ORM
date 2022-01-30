using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ORM.Core.Interfaces;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Caching
{
    public class StateTrackingCache : EntityCache, ICache
    {
        private readonly Dictionary<(Type, object), string> _hashes = new Dictionary<(Type, object), string>();

        /// <summary>
        /// Saves an entity to the cache
        /// </summary>
        /// <param name="entity"></param>
        public override void Save(object entity)
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
        public override void Remove(object entity)
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

            if (_hashes.ContainsKey((table.Type, pk)))
            {
                _hashes.Remove((table.Type, pk));
            }
        }
        
        /// <summary>
        /// Returns whether a given entity is different to its cached version.
        /// Returns true if the entity was not found in the cache or if it is null.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public override bool HasChanged(object? entity)
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

            // internal fields and foreign keys
            foreach (var column in table.Columns)
            {
                object? value = column.GetValue(entity);

                if (value is null)
                {
                    continue;
                }

                if (column.IsForeignKey)
                {
                    hashString += value;
                }
                else
                {
                    hashString += $"{column.Name}={value};";
                }
            }

            // external fields (collections)
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
                if (!propertyType.IsCollectionOfOneType()) continue;
                var collection = value as IEnumerable ?? new List<object>();
                hashString += $"{entityTable}=";    
                
                foreach (object? item in collection)
                {
                    object? itemPk = entityTable.PrimaryKey.GetValue(item);
                    hashString += $"{itemPk};";
                }
            }

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(hashString);
            byte[] hashBytes = SHA256.Create().ComputeHash(utf8Bytes);
            return Encoding.UTF8.GetString(hashBytes);
        }
    }
}