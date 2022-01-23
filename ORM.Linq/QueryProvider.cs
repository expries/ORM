using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;
using ORM.Linq.Interfaces;

namespace ORM.Linq
{
    /// <summary>
    /// Executes expressions against the current database connection
    /// </summary>
    public class QueryProvider : IQueryProvider
    {
        /// <summary>
        /// Translates expression trees to sql
        /// </summary>
        private readonly ILinqCommandBuilder _linqCommandBuilder;
        
        /// <summary>
        /// Loads relationships between entities
        /// </summary>
        private readonly ILazyLoader _lazyLoader;

        public QueryProvider(ILinqCommandBuilder linqCommandBuilder, ILazyLoader lazyLoader)
        {
            _linqCommandBuilder = linqCommandBuilder;
            _lazyLoader = lazyLoader;
        }
        
        /// <summary>
        /// Creates a queryable collection given an expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetElementTypeCustom();
            var typedDbSet = typeof(DbSet<>).MakeGenericType(elementType);
            object? dbSet = Activator.CreateInstance(typedDbSet, this, expression);

            if (dbSet is not IQueryable queryableDbSet)
            {
                throw new ArgumentException($"DbSet of type {dbSet?.GetType().Name} is not an IQueryable");
            }

            return queryableDbSet;
        }

        /// <summary>
        /// Creates a queryable of a type T given an expression
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new DbSet<T>(this, expression);
        }

        /// <summary>
        /// Executes an expression against the current database connection and returns the typed result
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Execute<T>(Expression expression)
        {
            dynamic result = Execute(expression);
            return result;
        }

        /// <summary>
        /// Executes an expression against the current database connection and returns the result
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object Execute(Expression expression)
        {
            var cmd = Translate(expression);
            var dataReader = cmd.ExecuteReader();
            
            var resultType = expression.Type.GetElementTypeCustom();
            var readerType = typeof(ObjectReader<>).MakeGenericType(resultType);
            object? reader = Activator.CreateInstance(readerType, dataReader, _lazyLoader);
            return reader ?? throw new OrmException("Failed to create object reader instance");
        }
        
        /// <summary>
        /// Translates an expression to sql
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private IDbCommand Translate(Expression expression)
        {
            return _linqCommandBuilder.Translate(expression);
        }
    }
}