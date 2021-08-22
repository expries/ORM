using System;
using System.Collections.Generic;
using ORM.Core.DataTypes;
using ORM.Core.SqlDialects;
using ORM.Infrastructure.Exceptions;
using ORM.Postgres.DataTypes;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresDataTypeMapper : IDbTypeMapper
    {
        private static readonly Dictionary<Type, Func<IDbDataType>> TypeMap = new()
        {
            [typeof(string)]   = () => new PostgresVarchar(PostgresVarchar.DefaultLength),
            [typeof(int)]      = () => new PostgresInt(),
            [typeof(double)]   = () => new PostgresDouble(),
            [typeof(DateTime)] = () => new PostgresDateTime(),
        };

        public IDbDataType Map(Type type)
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