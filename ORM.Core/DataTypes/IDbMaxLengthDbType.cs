namespace ORM.Core.DataTypes
{
    public interface IDbMaxLengthDbType : IDbType
    {
        public int Length { get; set; }
    }
}