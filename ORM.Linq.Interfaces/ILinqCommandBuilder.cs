using System.Data;
using System.Linq.Expressions;

namespace ORM.Linq.Interfaces
{
    /// <summary>
    /// Translates expression trees to SQL
    /// </summary>
    public interface ILinqCommandBuilder
    {
        /// <summary>
        /// Translates expression tree to SQL
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IDbCommand Translate(Expression? node);
    }
}