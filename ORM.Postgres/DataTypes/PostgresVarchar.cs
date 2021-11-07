namespace ORM.Postgres.DataTypes
{
    internal class PostgresVarchar : PostgresLengthRestrictedType
    {
        public static int DefaultLength => 255;

        public PostgresVarchar(int length) : base("VARCHAR", length)
        {
        }
    }
}