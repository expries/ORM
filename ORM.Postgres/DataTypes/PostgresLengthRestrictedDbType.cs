using ORM.Core.DataTypes;

namespace ORM.Postgres.DataTypes
{
    public class PostgresLengthRestrictedDbType : PostgresDataDbType, IDbMaxLengthDbType
    {
        public int Length { get; set; }
        
        protected PostgresLengthRestrictedDbType(string name, int length) : base(name)
        {
            Length = length;
        }
        
        public override string ToString()
        {
            return $"{Name}({Length})";
        }
    }
}