namespace ORM.Postgres.Interfaces
{
    public interface IDbMaxLengthDbType : IDbType
    {
        public int Length { get; set; }
    }
}