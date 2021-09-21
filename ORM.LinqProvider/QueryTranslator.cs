using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ORM.Core.Models;

namespace ORM.LinqToSql
{
    public class QueryTranslator : ExpressionVisitor
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public override Expression? Visit(Expression? node)
        {
            Console.WriteLine(node?.NodeType);
            return base.Visit(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            Console.WriteLine($"Unary: {node.NodeType}");
            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.And:
                    _sb.Append(" AND ");
                    break;
                
                case ExpressionType.Or:
                    _sb.Append(" OR ");
                    break;
                
                case ExpressionType.Equal:
                    _sb.Append(" = ");
                    break;
                
                case ExpressionType.NotEqual:
                    _sb.Append(" <> ");
                    break;
                
                case ExpressionType.LessThan:
                    _sb.Append(" < ");
                    break;
                
                case ExpressionType.LessThanOrEqual:
                    _sb.Append(" <= ");
                    break;
                
                case ExpressionType.GreaterThan:
                    _sb.Append(" > ");
                    break;
                
                case ExpressionType.GreaterThanOrEqual:
                    _sb.Append(" >= ");
                    break;
                
                default:
                    throw new NotSupportedException($"Binary operator {node.NodeType} is not supported.");
            }
            
            Visit(node.Right);

            Console.WriteLine($"Binary: {node.Method?.Name}");
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Console.WriteLine($"Method-Call: {node.Method.Name}");

            switch (node.Method.Name)
            {
                case "OrderBy":
                    _sb.Append("SELECT * FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T ORDER BY ");
                    Visit(node.Arguments[1]);
                    _sb.Append(" ASC");
                    return node;
                
                case "OrderByDescending":
                    _sb.Append("SELECT * FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T ORDER BY ");
                    Visit(node.Arguments[1]);
                    _sb.Append(" DESC");

                    return node;
                
                case "Average":
                    _sb.Append("SELECT AVG (");
                    Visit(node.Arguments[1]);
                    _sb.Append(") FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                    return node;
                
                case "Max":
                    _sb.Append("SELECT MAX (");
                    Visit(node.Arguments[1]);
                    _sb.Append(") FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                    return node;
                
                case "Min":
                    _sb.Append("SELECT MIN (");
                    Visit(node.Arguments[1]);
                    _sb.Append(") FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                    return node;
                
                case "Select":
                    _sb.Append("SELECT ");
                    Visit(node.Arguments[1]);
                    _sb.Append(" FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                    return node;
                
                case "Count":
                    _sb.Append("SELECT COUNT(*) FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                    return node;
                
                case "Any":
                    _sb.Append("SELECT EXISTS(");
                
                    _sb.Append("SELECT * FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                
                    // if a filter is set, generate where clause
                    if (node.Arguments.Count > 1)
                    {
                        _sb.Append(" WHERE ");
                        var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                        Visit(lambda?.Body);
                    }
                    
                    _sb.Append(')');
                    return node;
                
                case "All":
                    _sb.Append("SELECT NOT EXISTS(");
                
                    _sb.Append("SELECT * FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T WHERE NOT (");
                    
                    var allLambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                    Visit(allLambda?.Body);
                    
                    _sb.Append(')');

                    _sb.Append(')');
                    return node;
                
                case "Where" or "FirstOrDefault":
                    _sb.Append("SELECT * FROM (");
                    Visit(node.Arguments[0]);
                    _sb.Append(") AS T");
                
                    // if a filter is set, generate where clause
                    if (node.Arguments.Count > 1)
                    {
                        _sb.Append(" WHERE ");
                        var whereLambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                        Visit(whereLambda?.Body);
                    }
                    
                    if (node.Method.Name == "FirstOrDefault")
                    {
                        _sb.Append(" LIMIT 1");
                    }
                
                    return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                var table = new EntityTable(queryable.ElementType);
                var columnNames = table.Columns.Select(c => c.Name);
                string columnsSelection = string.Join(", ", columnNames);
                _sb.Append($"SELECT {columnsSelection} FROM {table.Name}");
                return node;
            }

            var type = node.Value?.GetType();
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    _sb.Append((bool) (node.Value ?? false) ? 1 : 0);
                    break;
                
                case TypeCode.String:
                    _sb.Append('\'');
                    _sb.Append(node.Value);
                    _sb.Append('\'');
                    break;
                
                default:
                    _sb.Append(node.Value);
                    break;
            }

            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Console.WriteLine($"Lambda: {node.NodeType}");
            return base.VisitLambda(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Console.WriteLine($"Parameter: {node.Name}");
            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Console.WriteLine($"Member: {node.Member.Name}");
            _sb.Append(node.Member.Name);
            return node;
        }

        public string Translate(Expression? node)
        {
            _sb.Clear();
            Visit(node);
            string sql = _sb.ToString();
            return sql;
        }
        
        private Expression StripQuotes(Expression e)
        {
            if (e.NodeType is not ExpressionType.Quote)
            {
                return e;
            }

            var unary = e as UnaryExpression;
            return unary?.Operand ?? e;
        }
    }
}