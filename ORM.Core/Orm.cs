using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ORM.Core.Helpers;
using ORM.Core.Models;
using ORM.Core.SqlDialects;

namespace ORM.Core
{
    public class Orm : IOrm
    {
        private readonly ISqlDialect _sqlDialect;

        private readonly StringBuilder _stringBuilder;

        public Orm(ISqlDialect dialect)
        {
            _sqlDialect = dialect;
            _stringBuilder = new StringBuilder();
        }
        
        public IEnumerable<Table> GetTables(Assembly assembly)
        {
            var entityTypes = AssemblyScanner.GetEntityTypes(assembly);
            var tables = entityTypes.Select(t => new EntityTable(t)).ToList();
            return tables;
        }

        public string GetTableSql(IEnumerable<Table> tables)
        {
            var sqlBuilder = new StringBuilder();
            var tablesToBeCreated = GetTablesToBeCreated(tables).ToList();
            var tableFk = GetForeignKeyPerTable(tablesToBeCreated);

            TranslateTablesToSql(tablesToBeCreated);
            TranslateForeignKeysToSql(tableFk);

            return sqlBuilder.ToString();
        }

        private void TranslateTablesToSql(IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                string tableSql = _sqlDialect.TableToSql(table);
                _stringBuilder.Append(tableSql);
            }
        }

        private void TranslateForeignKeysToSql(Dictionary<Table, List<ForeignKeyConstraint>> fkTable)
        {
            foreach ((var table, var fkList) in fkTable)
            {
                var foreignKeySql = fkList.Select(fk => _sqlDialect.ForeignKeyToSql(fk, table));
                foreignKeySql.ToList().ForEach(sql => _stringBuilder.Append(sql));
            }
        }

        private static Dictionary<Table, List<ForeignKeyConstraint>> GetForeignKeyPerTable(IEnumerable<Table> tables)
        {
            var tableFk = new Dictionary<Table, List<ForeignKeyConstraint>>();
            
            foreach (var table in tables)
            {
                tableFk[table] = new List<ForeignKeyConstraint>();
                table.ForeignKeys.ToList().ForEach(fk => tableFk[table].Add(fk));
            }

            return tableFk;
        }

        private static IEnumerable<Table> GetTablesToBeCreated(IEnumerable<Table> tables)
        {
            var tablesToBeCreated = new Dictionary<string, Table>();

            foreach (var table in tables)
            {
                tablesToBeCreated[table.Name] = table;
                table.Relationships.ToList().ForEach(tr => tablesToBeCreated[tr.Table.Name] = tr.Table);
                table.ForeignKeys.ToList().ForEach(fk => tablesToBeCreated[fk.TableTo.Name] = fk.TableTo);
            }

            return tablesToBeCreated.Values;
        }
    }
}