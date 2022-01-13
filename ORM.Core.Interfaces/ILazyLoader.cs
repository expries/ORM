using System.Collections.Generic;

namespace ORM.Core.Interfaces
{
    /// <summary>
    /// Loads entities connected by a relationship from the database.
    /// Lazy loading is then achieved by creating a lazy proxy that stores these functions in its backing fields
    /// </summary>
    public interface ILazyLoader
    {
        /// <summary>
        /// Takes the many-side and returns the entity of the one-side of a many-to-one relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public TOne LoadManyToOne<TMany, TOne>(TMany entity);

        /// <summary>
        /// Takes the one-side and returns the entities of the many-side of a one-to-many relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public List<TMany> LoadOneToMany<TOne, TMany>(TOne entity);
        
        /// <summary>
        /// Takes a many-side and returns the entities of the other many-side of a many-to-many relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TManyA"></typeparam>
        /// <typeparam name="TManyB"></typeparam>
        /// <returns></returns>
        public List<TManyB> LoadManyToMany<TManyA, TManyB>(TManyA entity);
    }
}