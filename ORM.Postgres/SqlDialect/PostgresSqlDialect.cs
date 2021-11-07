using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresSqlDialect : ISqlDialect
    {
        private readonly IDbTypeMapper _typeMapper;

        private StringBuilder _sb = new StringBuilder();
        
        public PostgresSqlDialect(IDbTypeMapper typeMapper)
        {
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

        public string TranslateSelect(EntityTable table)
        {
            var mappedColumns = table.Columns.Where(c => c.IsMapped).ToList();
            string columns = string.Join(',', mappedColumns.Select(c => c.Name));
            return $"SELECT {columns} FROM {table.Name}";
        }

        public string TranslateSelectById(EntityTable table, object pk)
        {
            var mappedColumns = table.Columns.Where(c => c.IsMapped).ToList();
            var pkColumn = mappedColumns.First(c => c.IsPrimaryKey);
            string columns = string.Join(',', mappedColumns.Select(c => c.Name));
            return $"SELECT {columns} FROM {table.Name} WHERE {pkColumn.Name} = {pk}";
        }

        public string TranslateInsert<T>(EntityTable table, T entity)
        {
            var mappedColumns = table.Columns.Where(c => c.IsMapped).ToList();
            var properties = typeof(T).GetProperties();
            
            var propertyValues = mappedColumns.Select(c =>
            {
                var property = properties.FirstOrDefault(p => new Column(p).Name == c.Name);
                object value = property?.GetValue(entity) ?? "NULL";
                
                if (value is string stringValue && stringValue != "NULL")
                {
                    value = $"'{stringValue}'";
                }

                return value;
            });
            
            string columns = string.Join(',', mappedColumns.Select(c => c.Name));
            string values = string.Join(", ", propertyValues);
            return $"INSERT INTO {table.Name} ({columns}) VALUES ({values})";
        }

        private void TranslateDropTable(Table table)
        {
            _sb
                .Append($"DROP TABLE IF EXISTS {table.Name} CASCADE;")
                .Append(Environment.NewLine);
        }
        
        private void TranslateCreateTable(Table table)
        {
            _sb
                .Append($"CREATE TABLE {table.Name}")
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
                .Append(column.Name)
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
        
        private void TranslateAddForeignKey(ForeignKeyConstraint foreignKeyConstraint, Table table)
        {
            _sb
                .Append("ALTER TABLE")
                .Append(' ')
                .Append(table.Name)
                .Append(' ')
                .Append("ADD CONSTRAINT")
                .Append(' ')
                .Append($"fk_{table.Name}_{foreignKeyConstraint.ColumnFrom.Name}_{foreignKeyConstraint.ColumnTo.Name}")
                .Append(' ')
                .Append("FOREIGN KEY")
                .Append('(')
                .Append(foreignKeyConstraint.ColumnFrom.Name)
                .Append(')')
                .Append(' ')
                .Append("REFERENCES")
                .Append(' ')
                .Append(foreignKeyConstraint.TableTo.Name)
                .Append('(')
                .Append(foreignKeyConstraint.ColumnTo.Name)
                .Append(')')
                .Append(' ')
                .Append("ON DELETE CASCADE")
                .Append(';')
                .Append(Environment.NewLine);
        }
    }
}