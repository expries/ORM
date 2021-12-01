using System.Collections.Generic;

namespace ORM.Postgres.SqlDialect
{
    public class Query
    {
        public string Sql { get; set; }
        
        public List<QueryParameter> Parameters { get; set; }

        public Query(string sql, List<QueryParameter> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }
    }
}