using System.Data;
using System.Linq.Expressions;

namespace ORM.LinqToSql
{
    public class SqlQueryProvider : QueryProvider
    {
        private readonly IDbConnection _connection;
        
        private readonly QueryTranslator _translator;

        public SqlQueryProvider(IDbConnection dbConnection, QueryTranslator translator)
        {
            _connection = dbConnection;
            _translator = translator;
        }
        
        public SqlQueryProvider(QueryTranslator translator)
        {
            _translator = translator;
        }
        
        public override object Execute(Expression expression)
        {
            string sql = Translate(expression);
            /*
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            var reader = cmd.ExecuteReader();
            var type = TypeSystem.GetElementType(expression.Type);
            */
            return 1;
        }

        private string Translate(Expression expression)
        {
            return _translator.Translate(expression);
        }
    }
}