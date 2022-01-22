using System.Data;
using System.Linq.Expressions;

namespace ORM.Linq.Interfaces
{
    /// <summary>
    /// Translates expression trees to sql
    /// </summary>
    public interface ILinqCommandBuilder
    {
        /// <summary>
        /// Translates expression tree to sql
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IDbCommand Translate(Expression? node);
    }
}