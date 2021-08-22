using ORM.Core.DataTypes;

namespace ORM.Postgres.DataTypes
{
    public class PostgresLengthRestrictedType : PostgresDataType, IDbMaxLengthType
    {
        public int Length { get; set; }
        
        protected PostgresLengthRestrictedType(string name, int length) : base(name)
        {
            Length = length;
        }
        
        public override string ToString()
        {
            return $"{Name}({Length})";
        }
    }
}