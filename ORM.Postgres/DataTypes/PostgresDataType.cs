using ORM.Core.DataTypes;

namespace ORM.Postgres.DataTypes
{
    public class PostgresDataType : IDbDataType
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