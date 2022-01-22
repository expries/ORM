using System.Linq.Expressions;
using System.Reflection;
using ORM.Core.Models.Extensions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public class MemberExpressionTranslator : ExpressionNodeTranslator<MemberExpression>
    {
        public MemberExpressionTranslator(LinqCommandBuilder translator) : base(translator)
        {
        }

        public override void Translate(MemberExpression node)
        {
            if (node.Member is PropertyInfo property)
            {
                Append($"\"{property.Name}\"");
            }
            else
            {
                Append(node.Member.Name);
            }
        }
    }
}