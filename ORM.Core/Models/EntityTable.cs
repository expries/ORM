using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Exceptions;
using ORM.Core.Helpers;

namespace ORM.Core.Models
{
    public class EntityTable : Table
    {
        private Type Type { get; }

        private readonly List<EntityTable> _rootTables = new List<EntityTable>();

        // Create new table baesd on the given entity type
        public EntityTable(Type entityType) : base(entityType.Name)
        {
            Type = entityType;
            ReadType(entityType);
        }
        
        // Create new table baesd on the given entity type
        // rootTables contains the tables that need to reference this table in order to be created
        private EntityTable(Type entityType, List<EntityTable> rootTables) : base(entityType.Name)
        {
            _rootTables = rootTables;
            Type = entityType;
            ReadType(entityType);
        }

        // Build table based on given entity type
        private void ReadType(Type entityType)
        {
            if (entityType.IsConvertibleToDbColumn())
            {
                throw new InvalidEntityException($"Type {entityType.Name} can not be represented by " +
                                                 $"a table. Please provide a complex type.");
            }

            var properties = entityType.GetProperties();
            var columnProperties = properties.Where(p => p.PropertyType.IsConvertibleToDbColumn());
            var entityProperties = properties.Where(p => !p.PropertyType.IsConvertibleToDbColumn());

            // first process properties which types can be converted into table columns
            foreach (var property in columnProperties)
            {
                AddColumn(property);
            }
            
            // check if entity has a mandatory primary key column
            var primaryKey = Columns.FirstOrDefault(c => c.IsPrimaryKey);

            if (primaryKey is null)
            {
                throw new InvalidEntityException($"Type {Type.Name} is missing a primary key column.");
            }

            // process properties which are of an entity type
            foreach (var property in entityProperties)
            {
                ReadEntityProperty(property);
            }
        }

        // process a property that is an entity itself
        private void ReadEntityProperty(PropertyInfo property)
        {
            // generate the entity's table
            var type = property.PropertyType;
            var entityType = type.IsCollectionOfAType() ? type.GetGenericArguments().First() : type;
            var entityTable = ResolveEntityTable(entityType);
            
            // get property on other entity for the current entity type
            var propertyOnOtherEntity = entityType.GetPropertyOfTypeFirstOrDefault(Type);

            if (propertyOnOtherEntity is null)
            {
                throw new InvalidEntityException($"Failed to navigate property {Type.Name}.{property.Name}: " +
                                                 $"type '{entityType.Name}' has no property of type '{Type.Name}'");
            }

            // relationship between entities is many-to-...
            if (type.IsCollectionOfAType())
            {
                AddManyTo(propertyOnOtherEntity, entityTable);
                return;
            }

            // relationship between entities is one-to-...
            AddOneTo(propertyOnOtherEntity, entityTable);
        }

        // Add one-to-... relationship to other entity table
        private void AddOneTo(PropertyInfo propertyOnOtherType, Table entityTable)
        {
            if (propertyOnOtherType.PropertyType.IsCollectionOfAType())
            {
                AddOneToMany(entityTable);
                return;
            }
            
            AddOneToOne(entityTable);
        }

        // Add many-to-... relationship to other entity table
        private void AddManyTo(PropertyInfo propertyOnOtherType, Table entityTable)
        {
            // check if other entity created many-to-many table and relationships
            if (RelationshipTo(entityTable) is TableRelationshipType.ManyToMany)
            {
                return;
            }
            
            if (propertyOnOtherType.PropertyType.IsCollectionOfAType())
            {
                AddManyToMany(entityTable);
                return;
            }
            
            AddManyToOne(entityTable);
        }
        
        // Add one-to-one relationship to other table 
        private void AddOneToOne(Table other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, true);
            AddRelationship(other, TableRelationshipType.OneToOne);
        }

        // Add one-to-many relationship to other table
        private void AddOneToMany(Table other)
        {
            AddRelationship(other, TableRelationshipType.OneToMany);
        }
        
        // Add many-to-one relationship to other table
        private void AddManyToOne(Table other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, false);
            AddRelationship(other, TableRelationshipType.ManyToOne);
        }
        
        // Add many-to-many relationship to other table
        private void AddManyToMany(Table other)
        {
            var fkTable = CreateManyToManyTable(other);
            AddRelationship(fkTable, TableRelationshipType.OneToMany);
            AddRelationship(other, TableRelationshipType.ManyToMany);
            other.AddRelationship(fkTable, TableRelationshipType.OneToMany);
            other.AddRelationship(this, TableRelationshipType.ManyToMany);
        }

        // Create helper table to represent many-to-many relationship to other table
        private Table CreateManyToManyTable(Table other)
        {
            var pkThis = Columns.First(c => c.IsPrimaryKey);
            var pkOther = other.Columns.First(c => c.IsPrimaryKey);
            var fkTable = new ForeignKeyTable(pkThis, pkOther, this, other);
            return fkTable;
        }

        // Get table for entity
        private Table ResolveEntityTable(Type entityType)
        {
            var table = _rootTables.FirstOrDefault(t => t.Type == entityType);
            
            if (table is not null)
            {
                return table;
            }
            
            _rootTables.Add(this);
            table = new EntityTable(entityType, _rootTables);
            _rootTables.Remove(this);
            
            return table;
        }
    }
}