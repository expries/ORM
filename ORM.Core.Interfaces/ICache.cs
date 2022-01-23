using System;
using System.Collections.Generic;

namespace ORM.Core.Interfaces
{
    /// <summary>
    /// Storage that enables caching
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Saves an entity to the cache
        /// </summary>
        /// <param name="entity"></param>
        public void Save(object entity);

        /// <summary>
        /// Removes an entity from the cache.
        /// Returns successfully whether entity was present in cache or not
        /// </summary>
        /// <param name="entity"></param>
        public void Remove(object entity);

        /// <summary>
        /// Gets an entity of a given type that has the provided primary key from the cache
        /// </summary>
        /// <param name="type"></param>
        /// <param name="primaryKey"></param>
        /// <returns></returns>
        public object? Get(Type type, object? primaryKey);

        /// <summary>
        /// Gets all the entities of a given type from the cache
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<object> GetAll(Type type);

        /// <summary>
        /// Returns whether a given entity is different to its cached version.
        /// Returns true if the entity was not found in the cache
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool HasChanged(object? entity);
    }
}