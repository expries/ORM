using System;
using System.Text;
using ORM.Core.DataTypes;
using ORM.Core.Models;
using ORM.Core.SqlDialects;

namespace ORM.Postgres.SqlDialect
{
    public class PostgreSqlDialect : ISqlDialect
    {
        private readonly IDbTypeMapper _typeMapper;
        
        public PostgreSqlDialect(IDbTypeMapper typeMapper)
        {
            _typeMapper = typeMapper;
        }
        
        public string ForeignKeyToSql(ForeignKeyConstraint foreignKeyConstraint, Table table)
        {
            StringBuilder sb = new StringBuilder();
            
            sb
                .Append("ALTER TABLE")
                .Append(' ')
                .Append(table.Name)
                .Append(' ')
                .Append("ADD CONSTRAINT")
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
                .Append(';')
                .Append(Environment.NewLine);

            return sb.ToString();
        }

        public string TableToSql(Table table)
        {
            StringBuilder sb = new StringBuilder();
            
            sb
                .Append($"CREATE TABLE {table.Name}")
                .Append(' ')
                .Append('(')
                .Append(Environment.NewLine);

            int i = 0;
            
            foreach (var column in table.Columns)
            {
                sb.Append('\t');
                ColumnToSql(column, sb);
                
                if (i < table.Columns.Count - 1)
                {
                    sb.Append(',');
                }

                sb.Append(Environment.NewLine);
                i++;
            }

            sb
                .Append(')')
                .Append(';')
                .Append(Environment.NewLine);
            
            return sb.ToString();
        }

        private void ColumnToSql(Column column, StringBuilder sb)
        {
            IDbType dbType = _typeMapper.Map(column.Type);
            
            if (column.MaxLength.HasValue && dbType is IDbMaxLengthDbType maxLengthType)
            {
                maxLengthType.Length = column.MaxLength.Value;
                dbType = maxLengthType;
            }

            sb
                .Append(column.Name)
                .Append(' ')
                .Append(dbType);

            if (column.IsPrimaryKey)
            {
                sb
                    .Append(' ')
                    .Append("PRIMARY KEY");
            }
                
            if (!column.IsNullable)
            {
                sb
                    .Append(' ')
                    .Append("NOT NULL");
            }

            if (column.IsUnique)
            {
                sb
                    .Append(' ')
                    .Append("UNIQUE");
            }
        }
    }
}