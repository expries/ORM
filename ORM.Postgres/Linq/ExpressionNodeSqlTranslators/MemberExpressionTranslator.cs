using System.Linq.Expressions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public class MemberExpressionTranslator : ExpressionNodeTranslator<MemberExpression>
    {
        public MemberExpressionTranslator(PostgresQueryTranslator translator) : base(translator)
        {
        }

        public override void Translate(MemberExpression node)
        {
            Append(node.Member.Name);
        }
    }
}