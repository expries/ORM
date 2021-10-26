using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ORM.Core
{
    public class DbSet<TEntity> : IQueryable<TEntity>, IOrderedQueryable<TEntity>
    {
        private readonly ICollection<TEntity> _entities = new List<TEntity>();

        public Type ElementType { get; } = typeof(TEntity);
        
        public Expression Expression { get; }
        
        public IQueryProvider Provider { get; }

        public DbSet(IQueryProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
                
            Provider = provider;
            Expression = Expression.Constant(this);
        }

        public DbSet(IQueryProvider provider, Expression expression)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            
            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }
            
            Provider = provider;
            Expression = expression;
        }
        
        public IEnumerator<TEntity> GetEnumerator()
        {
            var enumerable = Provider.Execute(Expression);
            return ((IEnumerable<TEntity>) enumerable)?.GetEnumerator() ?? _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var enumerable = Provider.Execute(Expression);
            return ((IEnumerable<TEntity>) enumerable)?.GetEnumerator() ?? _entities.GetEnumerator();
        }
    }
}