using System;
using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    public class ExternalField
    {
        public RelationshipType Relationship { get; set; }
        
        public EntityTable Table { get; set; }

        public ExternalField(EntityTable table, RelationshipType relationship)
        {
            Table = table;
            Relationship = relationship;
        }
    }
}