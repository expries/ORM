using System;
using System.Linq.Expressions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public class BinaryExpressionTranslator : ExpressionNodeTranslator<BinaryExpression>
    {
        public BinaryExpressionTranslator(LinqCommandBuilder translator) : base(translator)
        {
        }

        public override void Translate(BinaryExpression node)
        {
            Visit(node.Left);
            string operatorSql = GetSqlOperator(node.NodeType);
            Append($" {operatorSql} ");
            Visit(node.Right);
        }

        private static string GetSqlOperator(ExpressionType expressionOperator)
        {
            return expressionOperator switch
            {
                ExpressionType.And                => "AND",
                ExpressionType.Or                 => "OR",
                ExpressionType.Equal              => "=",
                ExpressionType.NotEqual           => "<>",
                ExpressionType.LessThan           => "<",
                ExpressionType.LessThanOrEqual    => "<=",
                ExpressionType.GreaterThan        => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                _ => throw new InvalidOperationException($"Binary operator {expressionOperator} is not supported.")
            };
        }
    }
}