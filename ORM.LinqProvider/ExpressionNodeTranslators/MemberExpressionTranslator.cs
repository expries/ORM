using System.Linq.Expressions;

namespace ORM.LinqToSql.ExpressionNodeTranslators
{
    public class MemberExpressionTranslator : ExpressionNodeTranslator<MemberExpression>
    {
        public MemberExpressionTranslator(QueryTranslator translator) : base(translator)
        {
        }

        public override void Translate(MemberExpression node)
        {
            Append(node.Member.Name);
        }
    }
}