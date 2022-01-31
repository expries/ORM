using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Npgsql;
using ORM.Core.Models;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;
using ORM.Linq.Interfaces;

namespace ORM.Postgres.Linq
{
    /// <summary>
    /// Translates expression trees to sql
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class LinqCommandBuilder : ExpressionVisitor, ILinqCommandBuilder
    {
        /// <summary>
        /// A string builder for creating the current commands' command text.
        /// </summary>
        private readonly StringBuilder _sql = new StringBuilder();

        /// <summary>
        /// The parameters of the current command
        /// </summary>
        private readonly List<IDbDataParameter> _parameters = new List<IDbDataParameter>();

        /// <summary>
        /// The current database connection
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Helper field for prefixing parameters for e.g. 'StartsWith'
        /// </summary>
        private string _constantPrefix = string.Empty;
        
        /// <summary>
        /// Helper field for suffixing parameters for e.g. 'StartsWith'
        /// </summary>
        private string _constantSuffix = string.Empty;

        public LinqCommandBuilder(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Translates expression tree to SQL
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IDbCommand Translate(Expression? node)
        {
            _sql.Clear();
            Visit(node);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = _sql.ToString();
            _parameters.ForEach(p => cmd.Parameters.Add(p));
            
            _parameters.Clear();
            _sql.Clear();
            
            Console.WriteLine($"Executing SQL statement: {Environment.NewLine}{cmd.CommandText}{Environment.NewLine}");
            return cmd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            string operatorSql = GetSqlOperator(node.NodeType);
            Visit(node.Left);
            _sql.Append($" {operatorSql} ");
            Visit(node.Right);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "OrderBy":
                    TranslateOrderByAscending(node);
                    return node;
                
                case "OrderByDescending":
                    TranslateOrderByDescending(node);
                    return node;
                
                case "Average":
                    TranslateAverage(node);
                    return node;
                
                case "Max":
                    TranslateMax(node);
                    return node;
                
                case "Min":
                    TranslateMin(node);
                    return node;
                
                case "Select":
                    TranslateSelect(node);
                    return node;
                
                case "Count":
                    TranslateCount(node);
                    return node;
                
                case "Any":
                    TranslateAny(node);
                    return node;
                
                case "All":
                    TranslateAll(node);
                    return node;
                
                case "Where":
                    TranslateWhere(node);
                    return node;
                    
                case "FirstOrDefault":
                    TranslateFirstOrDefault(node);
                    return node;
                
                case "Sum":
                    TranslateSum(node);
                    return node;
                
                case "StartsWith":
                    TranslateStartsWith(node);
                    return node;
                
                case "EndsWith":
                    TranslateEndsWithWith(node);
                    return node;
                
                default:
                    throw new OrmException($"Translating LINQ method {node.Method.Name} is not supported.");
            }
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                var table = queryable.ElementType.ToTable();
                string columnsSelection = GetColumns(table);
                
                _sql
                    .Append($"SELECT {columnsSelection}")
                    .Append(' ')
                    .Append($"FROM \"{table.Name}\"");
                
                return node;
            }

            object? value = node.Value;

            if (value is string stringValue)
            {
                value = $"{_constantPrefix}{stringValue}{_constantSuffix}";
            }

            if (value is char charValue)
            {
                value = $"{_constantPrefix}{charValue}{_constantSuffix}";
            }

            var parameter = CreateParameter(value);
            _sql.Append($"@{parameter.ParameterName}");
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo property)
            {
                var column = new Column(property);

                if (!column.IsMapped)
                {
                    throw new OrmException($"Can't filter by property '{property.Name}', because it is not mapped.");
                }
                
                _sql.Append($"\"{column.Name}\"");
            }
            else
            {
                _sql.Append(node.Member.Name);
            }

            return node;
        }
        
        private string GetColumns(EntityTable table)
        {
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");

            return string.Join(',', columns);
        }
        
        private void TranslateOrderByAscending(MethodCallExpression node)
        {
            _sql.Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T ORDER BY ");
            Visit(node.Arguments[1]);
            _sql.Append(" ASC");
        }

        private void TranslateOrderByDescending(MethodCallExpression node)
        {
            _sql.Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T ORDER BY ");
            Visit(node.Arguments[1]);
            _sql.Append(" DESC");
        }

        private void TranslateAverage(MethodCallExpression node)
        {
            _sql.Append("SELECT AVG (");
            Visit(node.Arguments[1]);
            _sql.Append(") FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
        }

        private void TranslateMax(MethodCallExpression node)
        {
            _sql.Append("SELECT MAX (");
            Visit(node.Arguments[1]);
            _sql.Append(") FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
        }

        private void TranslateMin(MethodCallExpression node)
        {
            _sql.Append("SELECT MIN (");
            Visit(node.Arguments[1]);
            _sql.Append(") FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
        }

        private void TranslateSelect(MethodCallExpression node)
        {
            _sql.Append("SELECT ");
            Visit(node.Arguments[1]);
            _sql.Append(" FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
        }

        private void TranslateCount(MethodCallExpression node)
        {
            _sql.Append("SELECT COUNT(*) FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
            
            // if a filter is set, generate where clause
            if (node.Arguments.Count > 1)
            {
                _sql.Append(" WHERE ");
                var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                Visit(lambda?.Body);
            }
        }

        private void TranslateAny(MethodCallExpression node)
        {
            _sql.Append("SELECT EXISTS(");
                
            _sql.Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
                
            // if a filter is set, generate where clause
            if (node.Arguments.Count > 1)
            {
                _sql.Append(" WHERE ");
                var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                Visit(lambda?.Body);
            }
                    
            _sql.Append(')');
        }

        private void TranslateAll(MethodCallExpression node)
        {
            _sql.Append("SELECT NOT EXISTS(");
                
            _sql.Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T WHERE NOT (");
            var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
            Visit(lambda?.Body);
            _sql.Append(')');

            _sql.Append(')');
        }
        
        private void TranslateWhere(MethodCallExpression node)
        {
            TranslateFilterExpression(node);
        }

        private void TranslateFirstOrDefault(MethodCallExpression node)
        {
            TranslateFilterExpression(node);
            _sql.Append(" LIMIT 1");
        }

        private void TranslateFilterExpression(MethodCallExpression node)
        {
            _sql.Append("SELECT * FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
                
            // if a filter is set, generate where clause
            if (node.Arguments.Count > 1)
            {
                _sql.Append(" WHERE ");
                var whereLambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
                Visit(whereLambda?.Body);
            }
        }

        private void TranslateSum(MethodCallExpression node)
        {
            var lambda = StripQuotes(node.Arguments[1]) as LambdaExpression;
            _sql.Append("SELECT SUM(");
            Visit(lambda?.Body);
            _sql.Append(") FROM (");
            Visit(node.Arguments[0]);
            _sql.Append(") AS T");
        }
        
        private void TranslateStartsWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sql.Append(" LIKE ");
            _constantSuffix = "%";
            Visit(node.Arguments[0]);
            _constantSuffix = string.Empty;
        }
        
        private void TranslateEndsWithWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sql.Append(" LIKE ");
            _constantPrefix = "%";
            Visit(node.Arguments[0]);
            _constantPrefix = string.Empty;
        }
        
        /// <summary>
        /// Returns the operand of a quote expression as an unary expression.
        /// If the given expression is not a quote expression, the expression is returned as-is.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private Expression StripQuotes(Expression e)
        {
            if (e.NodeType is not ExpressionType.Quote)
            {
                return e;
            }

            var unary = e as UnaryExpression;
            return unary?.Operand ?? e;
        }
        
        /// <summary>
        /// Returns the string presentation of an expression operator
        /// </summary>
        /// <param name="expressionOperator"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
                ExpressionType.OrElse             => "OR",
                _ => throw new InvalidOperationException($"Binary operator {expressionOperator} is not supported.")
            };
        }
        
        /// <summary>
        /// Creates a command parameter for a value and returns it
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private NpgsqlParameter CreateParameter<T>(T value)
        {
            int count = _parameters.Count;
            string name = $"p{count}";
            var parameter = new NpgsqlParameter(name, value);
            _parameters.Add(parameter);
            return parameter;
        }
    }
}