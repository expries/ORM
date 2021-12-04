using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core;
using ORM.Core.Interfaces;
using ORM.Core.Loading;
using ORM.Core.Models.Exceptions;
using ORM.Linq.Interfaces;

namespace ORM.Linq
{
    public class QueryProvider : IQueryProvider
    {
        private readonly IDbConnection _connection;
        
        private readonly IQueryTranslator _queryTranslator;
        
        private readonly ILazyLoader _lazyLoader;

        public QueryProvider(IDbConnection connection, IQueryTranslator queryTranslator, ILazyLoader lazyLoader)
        {
            _connection = connection;
            _queryTranslator = queryTranslator;
            _lazyLoader = lazyLoader;
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
            dynamic result = Execute(expression);
            return result;
        }

        public object Execute(Expression expression)
        {
            string sql = Translate(expression);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            var dataReader = cmd.ExecuteReader();
            
            var resultType = TypeSystem.GetElementType(expression.Type);
            var readerType = typeof(ObjectReader<>).MakeGenericType(resultType);
            object? reader = Activator.CreateInstance(readerType, dataReader, _lazyLoader);
            return reader ?? throw new OrmException("Failed to create object reader instance");
        }
        
        private string Translate(Expression expression)
        {
            return _queryTranslator.Translate(expression);
        }
    }
}