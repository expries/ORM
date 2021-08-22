using ORM.Core.SqlDialects;

namespace ORM.Core
{
    public interface IOrm
    {
        public IDbTypeMapper TypeMapper { get; }

        public void CreateTables();
    }
}