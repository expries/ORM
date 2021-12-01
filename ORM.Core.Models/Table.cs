using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    public class Table
    {
        public string Name { get; }

        public Column PrimaryKey { get; protected set; }
        
        public List<Column> Columns { get; } = new List<Column>();

        public List<ExternalField> ExternalFields { get; } = new List<ExternalField>();
        
        public List<ForeignKey> ForeignKeys { get; } = new List<ForeignKey>();
        
        public List<ForeignKeyTable> ForeignKeyTables { get; } = new List<ForeignKeyTable>();

        protected Table(string name)
        {
            Name = name;
        }
        
        public RelationshipType RelationshipTo(Type other)
        {
            var externalField = ExternalFields.FirstOrDefault(_ => _.Table.Type == other);
            return externalField?.Relationship ?? RelationshipType.None;
        }
        
        protected void AddColumn(PropertyInfo property)
        {
            var column = new Column(property);
            Columns.Add(column);
        }

        protected void AddExternalField(EntityTable table, RelationshipType relationship)
        {
            ExternalFields.RemoveAll(_ => _.Table.Type == table.Type);
            var field = new ExternalField(table, relationship);
            ExternalFields.Add(field);
        }

        protected void AddForeignKey(EntityTable other, bool nullable)
        {
            var pkColumn = other.Columns.First(c => c.IsPrimaryKey);
            string fkName = $"fk_{other.Name}_{pkColumn.Name}";
            var fkColumn = new Column(fkName, pkColumn.Type, isForeignKey: true, isNullable: nullable);
            var fkConstraint = new ForeignKey(fkColumn, pkColumn, other);
            Columns.Add(fkColumn);
            ForeignKeys.Add(fkConstraint);
        }
    }
}