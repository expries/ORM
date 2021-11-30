using System.Collections.Generic;
using System.Data;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    public interface ICommandBuilder
    {
        public string TranslateCreateTables(IEnumerable<Table> tables);
        
        public string TranslateDropTables(IEnumerable<Table> tables);

        public string TranslateAddForeignKeys(IEnumerable<Table> tables);

        public IDbCommand BuildSelect(EntityTable table);

        public IDbCommand BuildSelectById(EntityTable table, object pk);

        public string TranslateInsert<T>(EntityTable table, T entity);

        public string TranslateSelectManyToOne<TMany, TOne>(object pk);

        public string TranslateSelectOneToMany<TOne, TMany>(object pk);

        public string TranslateSelectManyToMany<TManyA, TManyB>(object pk);
    }
}