using System;
using System.Collections.Generic;
using System.Linq;
using ORM.Core.Interfaces;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Caching
{
    /// <summary>
    /// Entity storage used as cache
    /// </summary>
    public class EntityCache : ICache
    {
        protected readonly Dictionary<(Type, object), object> _cache = new Dictionary<(Type, object), object>();

        /// <summary>
        /// Saves an entity to the cache
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Save(object entity)
        {
            var table = entity.GetType().ToTable();
            object? pk = table.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                throw new OrmException("Primary key is not set on object.");
            }
            
            _cache[(table.Type, pk)] = entity;
        }

        /// <summary>
        /// Removes an entity from the cache.
        /// Returns successfully whether entity was present in cache or not
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Remove(object entity)
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
        public virtual bool HasChanged(object? entity) => true;
    }
}