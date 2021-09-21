using System;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core.Database;

namespace ORM.LinqToSql
{
    public abstract class QueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            var typedDbSet = typeof(DbSet<>).MakeGenericType(elementType);
            var dbSet = Activator.CreateInstance(typedDbSet, this, expression);
            return dbSet as IQueryable;
        }

        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new DbSet<T>(this, expression);
        }

        public T Execute<T>(Expression expression)
        {
            return (T) Execute(expression);
        }

        public abstract object Execute(Expression expression);
    }
}