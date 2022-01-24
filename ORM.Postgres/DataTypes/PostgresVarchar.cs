namespace ORM.Postgres.DataTypes
{
    /// <summary>
    /// Varchar datatype
    /// </summary>
    internal class PostgresVarchar : PostgresLengthRestrictedType
    {
        public static int DefaultLength => 255;

        public PostgresVarchar(int length) : base("VARCHAR", length)
        {
        }
    }
}