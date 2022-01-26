using System;
using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a table that is used to store a many-to-many relationship in a database
    /// </summary>
    public class ForeignKeyTable : Table
    {
        // The entity table for the first side of the many-to-many relationship
        public EntityTable TableA { get; set; }
        
        // The entity table for the second side of the many-to-many relationship
        public EntityTable TableB { get; set; }
        
        /// <summary>
        /// The column that has a foreign key constraint to TableA
        /// </summary>
        private Column FkColumnA { get; set; }
        
        /// <summary>
        /// The column that has a foreign key constraint to TableB
        /// </summary>
        private Column FkColumnB { get; set; }

        public ForeignKeyTable(EntityTable tableA, EntityTable tableB) : base($"fk_{tableA.Name}_{tableB.Name}")
        {
            // Set tables
            bool order = tableA.Type.GUID.GetHashCode() > tableB.Type.GUID.GetHashCode();
            TableA = order ? tableA : tableB;
            TableB = order ? tableB : tableA;

            // Set columns with foreign key constraint
            FkColumnA = new Column($"fk_{TableA.PrimaryKey.Name}", TableA.PrimaryKey.Type, this);
            FkColumnB = new Column($"fk_{TableB.PrimaryKey.Name}", TableA.PrimaryKey.Type, this);
            Columns.Add(FkColumnA);
            Columns.Add(FkColumnB);
            
            // Set foreign keys
            var fkA = new ForeignKey(FkColumnA, TableA.PrimaryKey, TableA);
            var fkB = new ForeignKey(FkColumnB, TableB.PrimaryKey, TableB);
            ForeignKeys.Add(fkA);
            ForeignKeys.Add(fkB);
            
            // Set relationships
            AddExternalField(TableA, RelationshipType.ManyToOne);
            AddExternalField(TableB, RelationshipType.ManyToOne);
            
            Name = $"fk_{TableA.Name}_{TableB.Name}";
        }

        /// <summary>
        /// Returns if this table represents the many-to-many relationship of exactly the two given types. Order
        /// is not important
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool MapsTypes(Type a, Type b)
        {
            return TableA.Type == a && TableB.Type == b || 
                   TableA.Type == b && TableB.Type == a;
        }
    }
}