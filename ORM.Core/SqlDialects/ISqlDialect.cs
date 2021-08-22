using ORM.Core.Models;

namespace ORM.Core.SqlDialects
{
    public interface ISqlDialect
    {
        public string ForeignKeyToSql(ForeignKeyConstraint foreignKeyConstraint, Table table);

        public string TableToSql(Table table);
    }
}