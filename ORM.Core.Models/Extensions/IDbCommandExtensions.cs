using System.Data;

namespace ORM.Core.Models.Extensions
{
    /// <summary>
    /// Extension methods for the IDbCommand interface
    /// </summary>
    public static class DbCommandExtensions
    {
        /// <summary>
        /// Adds a parameter to the command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="parameterName"></param>
        /// <param name="parameterValue"></param>
        public static void AddParameter(this IDbCommand cmd, string parameterName, object parameterValue)
        {
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            cmd.Parameters.Add(parameter);
        }
    }
}