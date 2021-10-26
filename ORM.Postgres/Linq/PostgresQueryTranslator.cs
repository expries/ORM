using System.Linq.Expressions;
using System.Text;
using ORM.Linq.Interfaces;
using ORM.Postgres.Linq.ExpressionNodeSqlTranslators;

namespace ORM.Postgres.Linq
{
    public class SqlQueryTranslator : ExpressionVisitor, IQueryTranslator
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public void Append(string str) => _sb.Append(str);

        public void Append(char c) => _sb.Append(c);

        public void Append(object obj) => _sb.Append(obj);
        
        public string Translate(Expression? node)
        {
            _sb.Clear();
            Visit(node);
            string sql = _sb.ToString();
            return sql;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            new BinaryExpressionTranslator(this).Translate(node);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            new MethodCallExpressionTranslator(this).Translate(node);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            new ConstantExpressionTranslator(this).Translate(node);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            new MemberExpressionTranslator(this).Translate(node);
            return node;
        }
    }
}