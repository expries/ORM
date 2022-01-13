using System.Collections.Generic;

namespace ORM.Postgres.SqlDialect
{
    /// <summary>
    /// A query stores a command's SQL as well as its parameters
    /// </summary>
    public class Query
    {
        /// <summary>
        /// The SQL or command text of the command
        /// </summary>
        public string Sql { get; set; }
        
        /// <summary>
        /// The parameters used in the command
        /// </summary>
        public List<QueryParameter> Parameters { get; set; }

        public Query(string sql, List<QueryParameter> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }
    }
}