using System.Collections.Generic;
using System.Reflection;

namespace ORM.Core.Interfaces
{
    /// <summary>
    /// Database context for performing methods in the current database context
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        /// Creates the database schema
        /// </summary>
        /// <param name="assembly"></param>
        public void EnsureCreated(Assembly? assembly = null);

        /// <summary>
        /// Saves an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        public void Save<T>(T entity);

        /// <summary>
        /// Get all entities of a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>();

        /// <summary>
        /// Get an entity by its primary key
        /// </summary>
        /// <param name="pk"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetById<T>(object pk);

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        /// <param name="pk"></param>
        public void DeleteById<T>(object pk);
    }
}