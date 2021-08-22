using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Helpers;
using ORM.Core.Models;
using ORM.Core.SqlDialects;

namespace ORM.Core
{
    public class Orm : IOrm
    {
        private readonly Assembly _assembly;

        private readonly ISqlDialect _sqlDialect;

        public IDbTypeMapper TypeMapper { get; }

        public Orm(Assembly assembly, ISqlDialect dialect, IDbTypeMapper typeMapper)
        {
            _assembly = assembly;
            _sqlDialect = dialect;
            TypeMapper = typeMapper;
        }

        public void CreateTables()
        {
            var entityTypes = AssemblyScanner.GetEntityTypes(_assembly);
            var tables = entityTypes.Select(t => new Table(TypeMapper, t)).ToList();

            var tableDict = new Dictionary<string, Table>();
            var tableFk = new Dictionary<Table, List<ForeignKeyConstraint>>();

            foreach (var table in tables)
            {
                tableDict[table.Name] = table;
                table.Relationships.Values.ToList().ForEach(tr => tableDict[tr.Table.Name] = tr.Table);
                table.ForeignKeys.ForEach(fk => tableDict[fk.TableTo.Name] = fk.TableTo);
            }
            
            foreach (var (_, table) in tableDict)
            {
                string tableSql = _sqlDialect.TableToSql(table);
                Console.WriteLine(tableSql);
                tableFk[table] = new List<ForeignKeyConstraint>();
                table.ForeignKeys.ForEach(fk => tableFk[table].Add(fk));
            }

            foreach ((var table, var fkList) in tableFk)
            {
                var foreignKeySql = fkList.Select(fk => _sqlDialect.ForeignKeyToSql(fk, table));
                foreignKeySql.ToList().ForEach(sql => Console.WriteLine(sql));
            }
        }
    }
}