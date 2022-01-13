using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ORM.Core
{
    /// <summary>
    /// Collection of entities stored in a database context
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class DbSet<TEntity> : IQueryable<TEntity>, IOrderedQueryable<TEntity>
    {
        /// <summary>
        /// Type of the elements stored in the collection
        /// </summary>
        public Type ElementType { get; } = typeof(TEntity);
        
        /// <summary>
        /// Expression to be executed by query provider
        /// </summary>
        public Expression Expression { get; }
        
        /// <summary>
        /// Translates expressions to sql
        /// </summary>
        public IQueryProvider Provider { get; }

        public DbSet(IQueryProvider provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        public DbSet(IQueryProvider provider, Expression expression)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));

            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }
        }
        
        /// <summary>
        /// Returns an enumerator that can be used to iterate through the collection.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IEnumerator<TEntity> GetEnumerator()
        {
            object? enumerable = Provider.Execute(Expression);
            return (enumerable as IEnumerable<TEntity>)?.GetEnumerator() ?? throw new ArgumentException();
        }

        /// <summary>
        /// Returns an IEnumerator object that can be used to iterate through the collection.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            object? enumerable = Provider.Execute(Expression);
            return (enumerable as IEnumerable<TEntity>)?.GetEnumerator() ?? GetEnumerator();
        }
    }
}