namespace ORM.Postgres.SqlDialect
{
    public class QueryParameter
    {
        public string Column { get; set; }
        
        public string Name { get; set; }
        
        public object Value { get; set; }

        public bool IsParameterized { get; set; }
        
        public QueryParameter(string column, string name, object value)
        {
            Column = column;
            Name = name;
            Value = value;
            IsParameterized = true;
        }

        public QueryParameter(string name, object value)
        {
            Column = string.Empty;
            Name = name;
            Value = value;
            IsParameterized = true;    
        }

        public QueryParameter(string column, string sql)
        {
            Column = column;
            Name = string.Empty;
            Value = sql;
            IsParameterized = false;
        }
    }
}