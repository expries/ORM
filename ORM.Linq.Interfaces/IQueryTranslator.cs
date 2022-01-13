using System.Linq.Expressions;

namespace ORM.Linq.Interfaces
{
    /// <summary>
    /// Translates expression trees to sql
    /// </summary>
    public interface IQueryTranslator
    {
        /// <summary>
        /// Translates expression tree to sql
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public string Translate(Expression? node);
    }
}