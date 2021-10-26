using System.Collections.Generic;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    public interface ISqlDialect
    {
        public string TranslateCreateTables(IEnumerable<Table> tables);
        
        public string TranslateDropTables(IEnumerable<Table> tables);

        public string TranslateAddForeignKeys(IEnumerable<Table> tables);
    }
}