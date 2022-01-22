using System;
using System.Linq;
using System.Linq.Expressions;
using ORM.Core.Models;
using ORM.Core.Models.Extensions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public class ConstantExpressionTranslator : ExpressionNodeTranslator<ConstantExpression>
    {
        public ConstantExpressionTranslator(LinqCommandBuilder translator) : base(translator)
        {
        }

        public override void Translate(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                var table = queryable.ElementType.ToTable();
                string columnsSelection = GetEscapedColumnsString(table);
                Append($"SELECT {columnsSelection} FROM \"{table.Name}\"");
                return;
            }

            var parameterName = AddParameter(node.Value);
            Append($"@{parameterName}");
        }
        
        private string GetUnescapedColumnsString(EntityTable table)
        {
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{c.Name}\"");

            return string.Join(',', columns);
        }
        
        private string GetEscapedColumnsString(EntityTable table)
        {
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");

            return string.Join(',', columns);
        }
    }
}