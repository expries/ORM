namespace ORM.Postgres.DataTypes
{
    /// <summary>
    /// DateTime data type
    /// </summary>
    internal class PostgresDateTime : PostgresDataType
    {
        public PostgresDateTime() : base("TIMESTAMP")
        {
        }
    }
}