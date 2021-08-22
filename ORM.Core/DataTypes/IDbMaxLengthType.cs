namespace ORM.Core.DataTypes
{
    public interface IDbMaxLengthType : IDbDataType
    {
        public int Length { get; set; }
    }
}