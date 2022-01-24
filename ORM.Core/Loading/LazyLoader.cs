using System.Collections.Generic;
using System.Linq;
using ORM.Core.Interfaces;

namespace ORM.Core.Loading
{
    /// <summary>
    /// Loads entities connected by a relationship from the database.
    /// Lazy loading is then achieved by creating a lazy proxy that stores the methods of this class
    /// in its backing fields
    /// </summary>
    public class LazyLoader : ILazyLoader
    {
        /// <summary>
        /// Builds database commands for the loading operations
        /// </summary>
        private readonly ICommandBuilder _commandBuilder;

        public LazyLoader(ICommandBuilder commandBuilder)
        {
            _commandBuilder = commandBuilder;
        }

        /// <summary>
        /// Takes the many-side and returns the entity of the one-side of a many-to-one relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public TOne LoadManyToOne<TMany, TOne>(TMany entity)
        {
            var cmd = _commandBuilder.BuildLoadManyToOne<TMany, TOne>(entity);
            var reader = cmd.ExecuteReader();
            var result = (TOne) new ObjectReader<TOne>(reader, this);
            return result;
        }

        /// <summary>
        /// Takes the one-side and returns the entities of the many-side of a one-to-many relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public List<TMany> LoadOneToMany<TOne, TMany>(TOne entity)
        {
            var cmd = _commandBuilder.BuildLoadOneToMany<TOne, TMany>(entity);
            var reader = cmd.ExecuteReader();
            var result =  new ObjectReader<TMany>(reader, this).ToList();
            return result;
        }

        /// <summary>
        /// Takes a many-side and returns the entities of the other many-side of a many-to-many relationship.
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TManyA"></typeparam>
        /// <typeparam name="TManyB"></typeparam>
        /// <returns></returns>
        public List<TManyB> LoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var cmd = _commandBuilder.BuildLoadManyToMany<TManyA, TManyB>(entity);
            var reader = cmd.ExecuteReader();
            var result =  new ObjectReader<TManyB>(reader, this).ToList();
            return result;
        }
    }
}