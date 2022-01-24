using System;

namespace ORM.Postgres.Interfaces
{
    /// <summary>
    /// Maps c# types to postgres data types
    /// </summary>
    public interface IDbTypeMapper
    {
        /// <summary>
        /// Maps an internal type to a postgres data type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IDbType Map(Type type);
    }
}