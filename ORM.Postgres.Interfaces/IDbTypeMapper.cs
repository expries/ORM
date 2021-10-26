using System;

namespace ORM.Postgres.Interfaces
{
    public interface IDbTypeMapper
    {
        public IDbType Map(Type type);
    }
}