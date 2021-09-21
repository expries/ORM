 using System.Collections.Generic;
 using System.Reflection;
 using ORM.Core.Models;

 namespace ORM.Core
{
    public interface IOrm
    {
        public IEnumerable<Table> GetTables(Assembly assembly);

        public string GetTableSql(IEnumerable<Table> tables);
    }
}