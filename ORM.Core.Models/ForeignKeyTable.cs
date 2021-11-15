using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    public class ForeignKeyTable : Table
    {
        public ForeignKeyTable(Column keyA, Column keyB, Table tableA, Table tableB) 
            : base($"fk_{tableA.Name}_{tableB.Name}")
        {
            // add columns
            var columnA = new Column($"fk_{keyA.Name}", keyA.Type);
            var columnB = new Column($"fk_{keyB.Name}", keyB.Type);
            Columns.Add(columnA);
            Columns.Add(columnB);
            
            // add foreign key constraints
            var fkA = new ForeignKeyConstraint(columnA, keyA, tableA);
            var fkB = new ForeignKeyConstraint(columnB, keyB, tableB);
            ForeignKeys.Add(fkA);
            ForeignKeys.Add(fkB);
            
            // add table relationships
            var relationshipA = new TableRelationship(tableA, RelationshipType.ManyToOne);
            var relationshipB = new TableRelationship(tableB, RelationshipType.ManyToOne);
            Relationships.Add(relationshipA);
            Relationships.Add(relationshipB);
        }
    }
}