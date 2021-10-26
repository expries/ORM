using System.Linq.Expressions;

namespace ORM.Linq.Interfaces
{
    public interface IQueryTranslator
    {
        public string Translate(Expression? node);
    }
}