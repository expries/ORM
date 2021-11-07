using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ORM.Core
{
    public class DbSet<TEntity> : IQueryable<TEntity>, IOrderedQueryable<TEntity>
    {
        public Type ElementType { get; } = typeof(TEntity);
        
        public Expression Expression { get; }
        
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
        
        public IEnumerator<TEntity> GetEnumerator()
        {
            object? enumerable = Provider.Execute(Expression);
            return (enumerable as IEnumerable<TEntity>)?.GetEnumerator() ?? throw new ArgumentException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            object? enumerable = Provider.Execute(Expression);
            return (enumerable as IEnumerable<TEntity>)?.GetEnumerator() ?? GetEnumerator();
        }
    }
}