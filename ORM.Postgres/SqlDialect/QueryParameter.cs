using System;

namespace ORM.Postgres.SqlDialect
{
    /// <summary>
    /// Represents a parameter used in a command
    /// </summary>
    public class QueryParameter
    {
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Value of the parameter
        /// </summary>
        public object Value { get; }

        public QueryParameter(string name, object? value)
        {
            Name = name;
            Value = value ?? DBNull.Value;
        }
    }
}