namespace ORM.Core.Models
{
    public class TableRelationship
    {
        public Table Table { get; }

        public TableRelationshipType Type { get; }

        public TableRelationship(Table table, TableRelationshipType relationshipType)
        {
            Table = table;
            Type = relationshipType;
        }
    }
}