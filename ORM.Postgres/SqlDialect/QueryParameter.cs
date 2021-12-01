using System.Collections.Generic;

namespace ORM.Postgres.SqlDialect
{
    public class QueryParameter
    {
        public string Column { get; set; }
        
        public string ParameterName { get; set; }
        
        public object ParameterValue { get; set; }

        public bool IsParameterized { get; set; }
        
        public QueryParameter(string column, string parameterName, object parameterValue)
        {
            Column = column;
            ParameterName = parameterName;
            ParameterValue = parameterValue;
            IsParameterized = true;
        }

        public QueryParameter(string parameterName, object parameterValue)
        {
            Column = string.Empty;
            ParameterName = parameterName;
            ParameterValue = parameterValue;
            IsParameterized = true;    
        }

        public QueryParameter(string column, string sql)
        {
            Column = column;
            ParameterName = string.Empty;
            ParameterValue = sql;
            IsParameterized = false;
        }
    }
}