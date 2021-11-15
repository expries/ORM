using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    public class TableRelationship
    {
        public Table Table { get; }

        public RelationshipType Type { get; }

        public TableRelationship(Table table, RelationshipType relationshipType)
        {
            Table = table;
            Type = relationshipType;
        }
    }
}