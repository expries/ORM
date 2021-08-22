using System;
using System.Text;
using ORM.Core.Models;
using ORM.Core.SqlDialects;

namespace ORM.Postgres.SqlDialect
{
    public class PostgreSqlDialect : ISqlDialect
    {
        public string ForeignKeyToSql(ForeignKeyConstraint foreignKeyConstraint, Table table)
        {
            var sb = new StringBuilder();
            
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
                .Append(';');

            return sb.ToString();
        }

        public string TableToSql(Table table)
        {
            var sb = new StringBuilder();
            
            sb
                .Append($"CREATE TABLE {table.Name}")
                .Append(' ')
                .Append('(')
                .Append(Environment.NewLine);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                sb.Append('\t');
                ColumnToSql(column, sb);
                
                if (i < table.Columns.Count - 1)
                {
                    sb.Append(',');
                }

                sb.Append(Environment.NewLine);
            }

            sb.Append(')').Append(';');
            return sb.ToString();
        }

        private void ColumnToSql(Column column, StringBuilder sb)
        {
            sb
                .Append(column.Name)
                .Append(' ')
                .Append(column.Type);

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