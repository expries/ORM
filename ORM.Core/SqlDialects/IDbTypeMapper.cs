using System;
using ORM.Core.DataTypes;

namespace ORM.Core.SqlDialects
{
    public interface IDbTypeMapper
    {
        public IDbType Map(Type type);
    }
}