using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a relationship to a table
    /// </summary>
    public class TableRelationship
    {
        /// <summary>
        /// The Table the relationship is formed with
        /// </summary>
        public Table Table { get; }

        /// <summary>
        /// Type of relationship
        /// </summary>
        public RelationshipType Type { get; }

        public TableRelationship(Table table, RelationshipType relationshipType)
        {
            Table = table;
            Type = relationshipType;
        }
    }
}