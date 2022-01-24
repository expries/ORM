using ORM.Postgres.Interfaces;

namespace ORM.Postgres.DataTypes
{
    /// <summary>
    /// Base class for postgres data types 
    /// </summary>
    internal class PostgresDataType : IDbType
    {
        /// <summary>
        /// Name of the data type
        /// </summary>
        protected string Name { get; set; }

        protected PostgresDataType(string name)
        {
            Name = name.ToUpper();
        }
        
        public override string ToString()
        {
            return Name;
        }
    }
}