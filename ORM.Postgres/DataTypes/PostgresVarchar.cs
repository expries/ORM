namespace ORM.Postgres.DataTypes
{
    public class PostgresVarchar : PostgresLengthRestrictedType
    {
        public static int DefaultLength => 255;

        public PostgresVarchar(int length) : base("VARCHAR", length)
        {
        }
    }
}