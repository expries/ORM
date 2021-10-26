using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ORM.Core.Models
{
    public class Table
    {
        public string Name { get; }
        
        public ICollection<Column> Columns { get; } = new List<Column>();

        public ICollection<ForeignKeyConstraint> ForeignKeys { get; } = new List<ForeignKeyConstraint>();

        public ICollection<TableRelationship> Relationships { get; } = new List<TableRelationship>();

        protected Table(string name)
        {
            Name = name;
        }

        public void AddRelationship(Table other, TableRelationshipType type)
        {
            // remove old relationships to table
            Relationships
                .Where(r => r.Table == other).ToList()
                .ForEach(x => Relationships.Remove(x));

            // add new relationship to table
            var relationship = new TableRelationship(other, type);
            Relationships.Add(relationship);
        }
        
        protected TableRelationshipType RelationshipTo(Table other)
        {
            var relationship = Relationships.FirstOrDefault(x => x.Table == other);
            return relationship?.Type ?? TableRelationshipType.None;
        }

        protected void AddColumn(PropertyInfo property)
        {
            var column = new Column(property);
            Columns.Add(column);
        }

        protected void AddForeignKey(string fkName, Table other, bool nullable)
        {
            var pkColumn = other.Columns.First(c => c.IsPrimaryKey);
            var fkColumn = new Column(fkName, pkColumn.Type, isForeignKey: true, isNullable: nullable);
            var foreignKeyConstraint = new ForeignKeyConstraint(fkColumn, pkColumn, other);

            Columns.Add(fkColumn);
            ForeignKeys.Add(foreignKeyConstraint);
        }
    }
}