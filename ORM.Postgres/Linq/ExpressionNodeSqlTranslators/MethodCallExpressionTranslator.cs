using System.Linq.Expressions;

namespace ORM.Postgres.Linq.ExpressionNodeSqlTranslators
{
    public class MethodCallExpressionTranslator : ExpressionNodeTranslator<MethodCallExpression>
    {
        public MethodCallExpressionTranslator(PostgresQueryTranslator translator) : base(translator)
        {
        }
        
        public override void Translate(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "OrderBy":
                    TranslateOrderByAscending(node);
                    return;
                
                case "OrderByDescending":
                    TranslateOrderByDescending(node);
                    return;
                
                case "Average":
                    TranslateAverage(node);
                    return;
                
                case "Max":
                    TranslateMax(node);
                    return;
                
                case "Min":
                    TranslateMin(node);
                    return;
                
                case "Select":
                    TranslateSelect(node);
                    return;
                
                case "Count":
                    TranslateCount(node);
                    return;
                
                case "Any":
                    TranslateAny(node);
                    return;
                
                case "All":
                    TranslateAll(node);
                    return;
                
                case "Where":
                    TranslateWhere(node);
                    return;
                    
                case "FirstOrDefault":
                    TranslateFirstOrDefault(node);
                    return;
            }
        }

        private void TranslateOrderByAscending(MethodCallExpression node)
        {
            Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T ORDER BY ");
            Visit(node.Arguments[1]);
            Append(" ASC");
        }

        private void TranslateOrderByDescending(MethodCallExpression node)
        {
            Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T ORDER BY ");
            Visit(node.Arguments[1]);
            Append(" DESC");
        }

        private void TranslateAverage(MethodCallExpression node)
        {
            Append("SELECT AVG (");
            Visit(node.Arguments[1]);
            Append(") FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
        }

        private void TranslateMax(MethodCallExpression node)
        {
            Append("SELECT MAX (");
            Visit(node.Arguments[1]);
            Append(") FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
        }

        private void TranslateMin(MethodCallExpression node)
        {
            Append("SELECT MIN (");
            Visit(node.Arguments[1]);
            Append(") FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
        }

        private void TranslateSelect(MethodCallExpression node)
        {
            Append("SELECT ");
            Visit(node.Arguments[1]);
            Append(" FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
        }

        private void TranslateCount(MethodCallExpression node)
        {
            Append("SELECT COUNT(*) FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
        }

        private void TranslateAny(MethodCallExpression node)
        {
            Append("SELECT EXISTS(");
                
            Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
                
            // if a filter is set, generate where clause
            if (node.Arguments.Count > 1)
            {
                Append(" WHERE ");
                var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                Visit(lambda?.Body);
            }
                    
            Append(')');
        }

        private void TranslateAll(MethodCallExpression node)
        {
            Append("SELECT NOT EXISTS(");
                
            Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T WHERE NOT (");
                    
            var allLambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
            Visit(allLambda?.Body);
                    
            Append(')');

            Append(')');
        }
        
        private void TranslateWhere(MethodCallExpression node)
        {
            TranslateFilterExpression(node);
        }

        private void TranslateFirstOrDefault(MethodCallExpression node)
        {
            TranslateFilterExpression(node);
            Append(" LIMIT 1");
        }

        private void TranslateFilterExpression(MethodCallExpression node)
        {
            Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            Append(") AS T");
                
            // if a filter is set, generate where clause
            if (node.Arguments.Count > 1)
            {
                Append(" WHERE ");
                var whereLambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                Visit(whereLambda?.Body);
            }
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