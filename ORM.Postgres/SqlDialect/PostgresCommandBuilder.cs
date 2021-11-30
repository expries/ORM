using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Extensions;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresCommandBuilder : ICommandBuilder
    {
        private readonly IDbConnection _connection;

        private readonly IDbTypeMapper _typeMapper;
        
        private StringBuilder _sb = new StringBuilder();
        
        public PostgresCommandBuilder(IDbConnection connection, IDbTypeMapper typeMapper)
        {
            _connection = connection;
            _typeMapper = typeMapper;
        }

        public string TranslateCreateTables(IEnumerable<Table> tables)
        {
            _sb = new StringBuilder();
            tables.ToList().ForEach(TranslateCreateTable);
            return _sb.ToString();
        }

        public string TranslateDropTables(IEnumerable<Table> tables)
        {
            _sb = new StringBuilder();
            tables.ToList().ForEach(TranslateDropTable);
            return _sb.ToString();
        }
        
        public string TranslateAddForeignKeys(IEnumerable<Table> tables)
        {
            _sb = new StringBuilder();
            
            foreach (var table in tables)
            {
                table.ForeignKeys.ToList().ForEach(fc => TranslateAddForeignKey(fc, table));
            }

            return _sb.ToString();
        }

        public IDbCommand BuildSelect(EntityTable table)
        {
            string selectSql = TranslateSelect(table);
            return GetCommand(selectSql);
        }
        
        public IDbCommand BuildSelectById(EntityTable table, object pk)
        {
            string selectByIdSql = TranslateSelectById(table, pk);
            return GetCommand(selectByIdSql);
        }
        
        private IDbCommand GetCommand(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }
        
        private string TranslateSelect(EntityTable table)
        {
            var tableList = new List<EntityTable> {table};
            table.BaseTables.ForEach(x => tableList.Add(x));
            
            string columns = GetEscapedColumnsString(tableList);
            string sql = $"SELECT {columns} FROM \"{table.Name}\"";
            
            foreach (var parentTable in table.BaseTables)
            {
                var fk = table.ForeignKeys.First(k => k.IsInheritanceKey && k.TableTo == parentTable);
                sql += $" JOIN \"{fk.TableTo.Name}\" ON \"{table.Name}\".\"{fk.ColumnFrom.Name}\" = \"{fk.TableTo.Name}\".\"{fk.ColumnTo.Name}\"";
            }
            
            return sql;
        }

        private string TranslateSelectById(EntityTable table, object pk)
        {
            string selectSql = TranslateSelect(table);
            return selectSql + $" WHERE \"{table.PrimaryKey.Name}\" = {pk}";
        }

        public string TranslateInsert<T>(EntityTable table, T entity)
        {
            string columns = GetUnescapedColumnsString(table);
            string values = GetValuesString(table, entity);
            return $"INSERT INTO \"{table.Name}\" ({columns}) VALUES ({values})";
        }

        public string TranslateSelectManyToOne<TMany, TOne>(object pk)
        {
            var manyTable = typeof(TMany).ToTable();
            var oneTable = typeof(TOne).ToTable();
            
            var columns = oneTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"t.\"{c.Name}\"");

            string columnString = string.Join(',', columns);
            
            var pkColumn = manyTable.Columns.First(c => c.IsPrimaryKey);
            var fkPkColumn = oneTable.Columns.First(c => c.IsPrimaryKey);
            var fk  = manyTable.ForeignKeys.First(c => c.TableTo.Name == oneTable.Name);
            var fkColumn = fk.ColumnFrom;

            return $"SELECT {columnString} FROM \"{manyTable.Name}\" " +
                   $"JOIN \"{oneTable.Name}\" t on t.\"{fkPkColumn.Name}\" = \"{manyTable.Name}\".\"{fkColumn.Name}\" " +
                   $"WHERE \"{manyTable.Name}\".\"{pkColumn.Name}\" = {pk}";
        }

        public string TranslateSelectOneToMany<TOne, TMany>(object pk)
        {
            var oneTable = typeof(TOne).ToTable();
            var manyTable = typeof(TMany).ToTable();
            
            string columns = GetEscapedColumnsString(manyTable);
            var pkColumn = oneTable.Columns.First(c => c.IsPrimaryKey);
            var fk  = manyTable.ForeignKeys.First(c => c.TableTo.Name == oneTable.Name);
            var fkColumn = fk.ColumnFrom;

            return $"SELECT {columns} FROM \"{manyTable.Name}\" " +
                   $"JOIN \"{oneTable.Name}\" t on t.\"{pkColumn.Name}\" = \"{manyTable.Name}\".\"{fkColumn.Name}\" " +
                   $"WHERE t.\"{pkColumn.Name}\" = {pk}";
        }

        public string TranslateSelectManyToMany<TManyA, TManyB>(object pk)
        {
            var manyATable = typeof(TManyA).ToTable();
            var manyBTable = typeof(TManyB).ToTable();

            var fkTable = manyATable.ForeignKeyTables.First(_ => 
                _.TableA.Type == typeof(TManyA) && _.TableB.Type == typeof(TManyB) || 
                _.TableA.Type == typeof(TManyB) && _.TableB.Type == typeof(TManyA));

            var fkTableA = fkTable.ForeignKeys.First(fc => 
                fc.TableTo is EntityTable t && t.Type == typeof(TManyA));
            
            var fkTableB = fkTable.ForeignKeys.First(fc => 
                fc.TableTo is EntityTable t && t.Type == typeof(TManyB));
            
            string columns = GetEscapedColumnsString(manyBTable);
            var pkColumnA = manyATable.Columns.First(c => c.IsPrimaryKey);
            var pkColumnB = manyBTable.Columns.First(c => c.IsPrimaryKey);

            return $"SELECT {columns} FROM \"{manyATable.Name}\" " +
                   $"JOIN \"{fkTable.Name}\" t ON \"{manyATable.Name}\".\"{pkColumnA.Name}\" = t.\"{fkTableA.ColumnFrom.Name}\" " +
                   $"JOIN \"{manyBTable.Name}\" ON t.\"{fkTableB.ColumnFrom.Name}\" = \"{manyBTable.Name}\".\"{pkColumnB.Name}\"";
        }

        private void TranslateDropTable(Table table)
        {
            _sb
                .Append($"DROP TABLE IF EXISTS \"{table.Name}\" CASCADE;")
                .Append(Environment.NewLine);
        }
        
        private void TranslateCreateTable(Table table)
        {
            _sb
                .Append($"CREATE TABLE \"{table.Name}\"")
                .Append(' ')
                .Append('(')
                .Append(Environment.NewLine);

            int i = 0;
            
            foreach (var column in table.Columns)
            {
                _sb.Append('\t');
                TranslateColumn(column);
                
                if (i < table.Columns.Count - 1)
                {
                    _sb.Append(',');
                }

                _sb.Append(Environment.NewLine);
                i++;
            }

            _sb
                .Append(')')
                .Append(';')
                .Append(Environment.NewLine);
        }
        
        private void TranslateColumn(Column column)
        {
            var dbType = _typeMapper.Map(column.Type);
            
            if (column.MaxLength.HasValue && dbType is IDbMaxLengthDbType maxLengthType)
            {
                maxLengthType.Length = column.MaxLength.Value;
                dbType = maxLengthType;
            }

            _sb
                .Append($"\"{column.Name}\"")
                .Append(' ')
                .Append(dbType);

            if (column.IsPrimaryKey)
            {
                _sb
                    .Append(' ')
                    .Append("PRIMARY KEY");
            }
                
            if (!column.IsNullable)
            {
                _sb
                    .Append(' ')
                    .Append("NOT NULL");
            }

            if (column.IsUnique)
            {
                _sb
                    .Append(' ')
                    .Append("UNIQUE");
            }
        }
        
        private void TranslateAddForeignKey(ForeignKey foreignKey, Table table)
        {
            _sb
                .Append("ALTER TABLE")
                .Append(' ')
                .Append($"\"{table.Name}\"")
                .Append(' ')
                .Append("ADD CONSTRAINT")
                .Append(' ')
                .Append($"\"fk_{table.Name}_{foreignKey.ColumnFrom.Name}_{foreignKey.ColumnTo.Name}\"")
                .Append(' ')
                .Append("FOREIGN KEY")
                .Append('(')
                .Append($"\"{foreignKey.ColumnFrom.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("REFERENCES")
                .Append(' ')
                .Append($"\"{foreignKey.TableTo.Name}\"")
                .Append('(')
                .Append($"\"{foreignKey.ColumnTo.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("ON DELETE CASCADE")
                .Append(';')
                .Append(Environment.NewLine);
        }
        
        private static string GetEscapedColumnsString(EntityTable table)
        {
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");

            return string.Join(',', columns);
        }
        
        private static string GetUnescapedColumnsString(EntityTable table)
        {
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{c.Name}\"");

            return string.Join(',', columns);
        }
        
        private static string GetUnescapedColumnsString(List<EntityTable> tables)
        {
            var columns = new List<string>();
            
            foreach (var table in tables)
            {
                table.Columns
                    .Where(c => c.IsMapped)
                    .Select(c => $"\"{c.Name}\"")
                    .ToList()
                    .ForEach(s => columns.Add(s));
            }

            return string.Join(',', columns);
        }
        
        private static string GetEscapedColumnsString(List<EntityTable> tables)
        {
            var columns = new List<string>();
            
            foreach (var table in tables)
            {
                table.Columns
                    .Where(c => c.IsMapped)
                    .Select(c => $"\"{table.Name}\".\"{c.Name}\"")
                    .ToList()
                    .ForEach(s => columns.Add(s));
            }

            return string.Join(',', columns);
        }

        private string GetValuesString<T>(EntityTable table, T entity)
        {
            var properties = typeof(T).GetProperties();
            var values = table.Columns.Where(c => c.IsMapped).Select(c =>
            {
                var property = properties.FirstOrDefault(p => new Column(p).Name == c.Name);
                object value = property?.GetValue(entity) ?? DBNull.Value;
                return GetValue(value);
            });

            return string.Join(',', values);
        }

        private object GetValue(object value)
        {
            if (value is DBNull)
            {
                value = "NULL";
            }
            
            if (value is string stringValue && stringValue != "NULL")
            {
                value = $"'{stringValue}'";
            }

            if (value is DateTime dateTime)
            {
                value = $"TIMESTAMP '{dateTime.Year}-{dateTime.Month}-{dateTime.Day} {dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}'";
            }

            return value;
        }
    }
}