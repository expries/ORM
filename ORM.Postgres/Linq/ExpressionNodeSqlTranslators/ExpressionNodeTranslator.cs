#nullable enable
using System.Linq.Expressions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public abstract class ExpressionNodeTranslator<T>
    {
        private readonly PostgresQueryTranslator _translator;
        
        protected ExpressionNodeTranslator(PostgresQueryTranslator translator)
        {
            _translator = translator;
        }

        protected void Visit(Expression? node) => _translator.Visit(node);

        protected void Append(string sqlString) => _translator.Append(sqlString);

        protected void Append(char sqlChar) => _translator.Append(sqlChar);

        protected void Append(object obj) => _translator.Append(obj);

        public abstract void Translate(T node);
    }
}