using ORM.Core.DataTypes;

namespace ORM.Postgres.DataTypes
{
    public class PostgresDataDbType : IDbType
    {
        protected string Name { get; set; }

        protected PostgresDataDbType(string name)
        {
            Name = name.ToUpper();
        }
        
        public override string ToString()
        {
            return Name;
        }
    }
}