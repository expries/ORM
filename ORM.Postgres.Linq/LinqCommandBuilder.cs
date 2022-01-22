using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using Npgsql;
using ORM.Linq.Interfaces;
using ORM.Postgres.Linq.ExpressionNodeSqlTranslators;

namespace ORM.Postgres.Linq
{
    public class LinqCommandBuilder : ExpressionVisitor, ILinqCommandBuilder
    {
        private readonly StringBuilder _sql = new StringBuilder();

        private readonly List<NpgsqlParameter> _parameters = new List<NpgsqlParameter>();

        private readonly IDbConnection _connection;

        public LinqCommandBuilder(IDbConnection connection)
        {
            _connection = connection;
        }

        public void Append(string str) => _sql.Append(str);

        public void Append(char c) => _sql.Append(c);

        public void Append(object obj) => _sql.Append(obj);

        public string AddParameter<T>(T value)
        {
            var count = _parameters.Count;
            var name = $"p{count}";
            var parameter = new NpgsqlParameter(name, value);
            _parameters.Add(parameter);
            return name;
        }
        
        public IDbCommand Translate(Expression? node)
        {
            _sql.Clear();
            Visit(node);
            
            var cmd = _connection.CreateCommand();
            cmd.CommandText = _sql.ToString();
            _parameters.ForEach(p => cmd.Parameters.Add(p));
            
            Console.WriteLine(cmd.CommandText);
            return cmd;
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