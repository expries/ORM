using System;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core.Models;

namespace ORM.LinqToSql.ExpressionNodeTranslators
{
    public class ConstantExpressionTranslator : ExpressionNodeTranslator<ConstantExpression>
    {
        public ConstantExpressionTranslator(QueryTranslator translator) : base(translator)
        {
        }

        public override void Translate(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                var table = new EntityTable(queryable.ElementType);
                var columnNames = table.Columns.Select(c => c.Name);
                string columnsSelection = string.Join(", ", columnNames);
                Append($"SELECT {columnsSelection} FROM {table.Name}");
                return;
            }

            var type = node.Value?.GetType();
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    Append((bool) (node.Value ?? false) ? 1 : 0);
                    break;
                
                case TypeCode.String:
                    Append('\'');
                    Append(node.Value);
                    Append('\'');
                    break;
                
                default:
                    Append(node.Value);
                    break;
            }
        }
    }
}