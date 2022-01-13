using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a relationship with another entity
    /// </summary>
    public class ExternalField
    {
        /// <summary>
        /// Specifies the kind of relationship this external field represents
        /// </summary>
        public RelationshipType Relationship { get; set; }
        
        /// <summary>
        /// The type this relationship points to.
        /// </summary>
        public EntityTable Table { get; set; }
        
        public ExternalField(EntityTable table, RelationshipType relationship)
        {
            Table = table;
            Relationship = relationship;
        }
    }
}