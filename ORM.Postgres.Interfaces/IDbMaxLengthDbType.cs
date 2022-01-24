namespace ORM.Postgres.Interfaces
{
    /// <summary>
    /// Base class for postgres data types that have a maximum character length
    /// </summary>
    public interface IDbMaxLengthDbType : IDbType
    {
        public int Length { get; set; }
    }
}