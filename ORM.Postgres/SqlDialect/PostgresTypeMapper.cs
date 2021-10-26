using System;
using System.Collections.Generic;
using ORM.Core.Models.Exceptions;
using ORM.Postgres.DataTypes;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresDataTypeMapper : IDbTypeMapper
    {
        private static readonly Dictionary<Type, Func<IDbType>> TypeMap = new Dictionary<Type, Func<IDbType>>
        {
            [typeof(string)]   = () => new PostgresVarchar(PostgresVarchar.DefaultLength),
            [typeof(int)]      = () => new PostgresInt(),
            [typeof(double)]   = () => new PostgresDouble(),
            [typeof(DateTime)] = () => new PostgresDateTime(),
        };

        public IDbType Map(Type type)
        {
            if (!TypeMap.ContainsKey(type))
            {
                throw new TypeNotConvertableException($"Type {type.Name} is not convertable to a postgres type");
            }

            var postgresType = TypeMap[type].Invoke();
            return postgresType;
        }
    }
}