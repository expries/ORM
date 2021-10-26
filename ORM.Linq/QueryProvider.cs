using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core;
using ORM.Linq.Interfaces;

namespace ORM.Linq
{
    public class QueryProvider : IQueryProvider
    {
        private readonly IDbConnection _connection;

        private readonly IQueryTranslator _queryTranslator;
        
        public QueryProvider(IDbConnection connection, IQueryTranslator queryTranslator)
        {
            _connection = connection;
            _queryTranslator = queryTranslator;
        }
        
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

        public object Execute(Expression expression)
        {
            string sql = Translate(expression);
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            var type = TypeSystem.GetElementType(expression.Type);
            return 1;
        }
        
        private string Translate(Expression expression)
        {
            return _queryTranslator.Translate(expression);
        }
    }
}