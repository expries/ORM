using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Enums;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a database table
    /// </summary>
    public class Table
    {
        /// <summary>
        /// Name of the table
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Columns in the table
        /// </summary>
        public List<Column> Columns { get; } = new List<Column>();

        /// <summary>
        /// External fields that represent relationships to other tables
        /// </summary>
        public List<ExternalField> ExternalFields { get; } = new List<ExternalField>();
        
        /// <summary>
        /// Foreign keys in the table
        /// </summary>
        public List<ForeignKey> ForeignKeys { get; } = new List<ForeignKey>();

        protected Table(string name)
        {
            Name = name;
        }
        
        /// <summary>
        /// Gets the type of relationship the current type has to any given type
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public RelationshipType RelationshipTo(Type other)
        {
            var externalField = ExternalFields.FirstOrDefault(_ => _.Table.Type == other);
            return externalField?.Relationship ?? RelationshipType.None;
        }
        
        /// <summary>
        /// Adds an column for a property to the table
        /// </summary>
        /// <param name="property"></param>
        protected void AddColumn(PropertyInfo property)
        {
            var column = new Column(property);
            Columns.Add(column);
        }

        /// <summary>
        /// Adds an external field that represents an relationship to the type of the given entity table.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="relationship"></param>
        protected void AddExternalField(EntityTable table, RelationshipType relationship)
        {
            ExternalFields.RemoveAll(_ => _.Table.Type == table.Type);
            var field = new ExternalField(table, relationship);
            ExternalFields.Add(field);
        }

        /// <summary>
        /// Adds a foreign key toe the given entity table
        /// </summary>
        /// <param name="other"></param>
        /// <param name="nullable"></param>
        protected void AddForeignKey(EntityTable other, bool nullable)
        {
            string fkName = $"fk_{other.Name}_{other.PrimaryKey.Name}";
            var fkColumn = new Column(fkName, other.PrimaryKey.Type, other, isForeignKey: true, isNullable: nullable);
            var fkConstraint = new ForeignKey(fkColumn, other.PrimaryKey, other);
            Columns.Add(fkColumn);
            ForeignKeys.Add(fkConstraint);
        }
    }
}