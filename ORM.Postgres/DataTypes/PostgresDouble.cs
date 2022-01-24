namespace ORM.Postgres.DataTypes
{
    /// <summary>
    /// Double data type
    /// </summary>
    internal class PostgresDouble : PostgresDataType
    {
        public PostgresDouble() : base("DOUBLE PRECISION")
        {
        }
    }
}