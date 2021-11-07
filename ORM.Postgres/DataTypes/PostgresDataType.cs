using ORM.Postgres.Interfaces;

namespace ORM.Postgres.DataTypes
{
    internal class PostgresDataType : IDbType
    {
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