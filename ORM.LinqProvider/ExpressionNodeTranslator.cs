#nullable enable
using System.Linq.Expressions;

namespace ORM.LinqToSql
{
    public abstract class ExpressionNodeTranslator<T>
    {
        private readonly QueryTranslator _translator;
        
        protected ExpressionNodeTranslator(QueryTranslator translator)
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