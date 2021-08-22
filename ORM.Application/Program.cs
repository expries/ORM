using ORM.Core;
using ORM.Postgres.SqlDialect;

namespace ORM.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            var sqlDialect = new PostgreSqlDialect();
            var typeConverter = new PostgresDataTypeMapper();

            var orm = new Orm(typeof(Program).Assembly, sqlDialect, typeConverter);
            orm.CreateTables();
        }
    }
}