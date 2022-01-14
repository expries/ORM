using System;
using System.Collections.Generic;
using System.Data;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    /// <summary>
    /// Builds commands for database operations
    /// </summary>
    public interface ICommandBuilder
    {
        /// <summary>
        /// Builds a commands to create the given list of tables in a database
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDbCommand BuildEnsureCreated(List<Table> tables);

        /// <summary>
        /// Builds a command to get all entities of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildGetAll<T>();

        /// <summary>
        /// Builds a command ot get an entity by its primary key 
        /// </summary>
        /// <param name="pk"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildGetById<T>(object pk);

        /// <summary>
        /// Builds a command to save an entity to the database
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildSave<T>(T entity);

        /// <summary>
        /// Builds a command to delete an entity by its primary key
        /// </summary>
        /// <param name="pk"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildDeleteById<T>(object pk);

        /// <summary>
        /// Builds a command to remove references between an entity and a reference type that share a
        /// many to many relationship by deleting rows in their corresponding joining table.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="referenceType"></param>
        /// <returns></returns>
        public IDbCommand BuildRemoveManyToManyReferences(object entity, Type referenceType);
        
        /// <summary>
        /// Builds a command to save the references between two entities that share a many to many
        /// relationship by inserting rows in the corresponding joining table 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="referenceType"></param>
        /// <param name="referencePrimaryKeys"></param>
        /// <returns></returns>
        public IDbCommand BuildSaveManyToManyReferences(object entity, Type referenceType, List<object> referencePrimaryKeys);

        /// <summary>
        /// Builds a command to load an entity that is the one-side of a many-to-one relationship
        /// given the entity of the many side
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadManyToOne<TMany, TOne>(TMany entity);

        /// <summary>
        /// Builds a command to load the entities that are the many-side of a one-to-many relationship given
        /// an entity of the one-side
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TOne"></typeparam>
        /// <typeparam name="TMany"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadOneToMany<TOne, TMany>(TOne entity);

        /// <summary>
        /// Builds a command to load the entities of a related type in a many-to-many relationship, given an
        /// entity of the other side of the relationship
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TManyA"></typeparam>
        /// <typeparam name="TManyB"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadManyToMany<TManyA, TManyB>(TManyA entity);
    }
}