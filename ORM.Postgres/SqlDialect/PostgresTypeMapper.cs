using System;
using System.Collections.Generic;
using ORM.Core.Models.Exceptions;
using ORM.Postgres.DataTypes;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    /// <summary>
    /// Maps c# types to postgres data types
    /// </summary>
    public class PostgresDataTypeMapper : IDbTypeMapper
    {
        /// <summary>
        /// Type mapping dictionary
        /// </summary>
        private static readonly Dictionary<Type, Func<IDbType>> TypeMap = new Dictionary<Type, Func<IDbType>>
        {
            [typeof(string)]   = () => new PostgresVarchar(PostgresVarchar.DefaultLength),
            [typeof(int)]      = () => new PostgresInt(),
            [typeof(long)]     = () => new PostgresInt(),
            [typeof(double)]   = () => new PostgresDouble(),
            [typeof(DateTime)] = () => new PostgresDateTime(),
        };

        /// <summary>
        /// Maps an internal type to a postgres data type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="UnknownTypeException"></exception>
        public IDbType Map(Type type)
        {
            if (!TypeMap.ContainsKey(type))
            {
                throw new UnknownTypeException($"Type {type.Name} is not convertable to a postgres type");
            }

            var postgresType = TypeMap[type].Invoke();
            return postgresType;
        }
    }
}