using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Models
{
    public class EntityTable : Table
    {
        public Type Type { get; }

        private readonly List<EntityTable> _rootTables = new List<EntityTable>();

        /// <summary>
        /// Create new table based on the given entity type
        /// </summary>
        /// <param name="entityType"></param>
        public EntityTable(Type entityType) : base(entityType.Name)
        {
            Type = entityType;
            ReadType(entityType);
        }

        // Create new table based on the given entity type
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
            if (entityType.IsInternalType())
            {
                throw new InvalidEntityException($"Type {entityType.Name} can not be represented by " +
                                                 $"a table. Please provide a complex type.");
            }

            var properties = entityType.GetProperties();
            var columnProperties = properties.Where(p => p.PropertyType.IsInternalType());
            var externalFieldProperties = properties.Where(p => !p.PropertyType.IsInternalType());

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
            foreach (var property in externalFieldProperties)
            {
                ReadExternalProperty(property);
            }
        }

        // process a property that is an entity itself
        private void ReadExternalProperty(PropertyInfo property)
        {
            // generate the entity's table
            var type = property.PropertyType;
            var entityType = type.IsCollectionOfOneType() ? type.GetGenericArguments().First() : type;
            var entityTable = GetEntityTable(entityType);
            
            if (HasOneToOneRelationship(type, entityType))
            {
                AddOneToOne(entityTable);
            }

            if (HasOneToManyRelationship(type, entityType))
            {
                AddOneToMany(entityTable);
            }
            
            if (HasManyToOneRelationship(type, entityType))
            {
                AddManyToOne(entityTable);
            }
            
            if (HasManyToManyRelationship(type, entityType) && !CalledByTable(entityType))
            {
                AddManyToMany(entityTable);
            }
        }

        // Add one-to-one relationship to other table 
        private void AddOneToOne(EntityTable other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, true);
            AddExternalField(other, RelationshipType.OneToOne);
        }

        // Add one-to-many relationship to other table
        private void AddOneToMany(EntityTable other)
        {
            AddExternalField(other, RelationshipType.OneToMany);
        }
        
        // Add many-to-one relationship to other table
        private void AddManyToOne(EntityTable other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, false);
            AddExternalField(other, RelationshipType.ManyToOne);
        }
        
        // Add many-to-many relationship to other table
        private void AddManyToMany(EntityTable other)
        {
            AddForeignKeyTable(other);
            AddExternalField(other, RelationshipType.ManyToMany);
            other.AddExternalField(this, RelationshipType.ManyToMany);
        }

        // Add helper table to represent many-to-many relationship to other table
        private void AddForeignKeyTable(EntityTable other)
        {
            var fkTable = new ForeignKeyTable(this, other);
            ForeignKeyTables.Add(fkTable);
        }
        
        private bool HasOneToOneRelationship(Type propertyType, Type entityType)
        {
            if (propertyType.IsCollectionOfOneType())
            {
                return false;
            }
            
            var navigatedProperty = propertyType.GetNavigatedProperty(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return !navigatedProperty.PropertyType.IsCollectionOfOneType();
        }

        private bool HasOneToManyRelationship(Type propertyType, Type entityType)
        {
            if (!propertyType.IsCollectionOfOneType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetNavigatedProperty(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return !navigatedProperty.PropertyType.IsCollectionOfOneType();
        }
        
        private bool HasManyToOneRelationship(Type propertyType, Type entityType)
        {
            if (propertyType.IsCollectionOfOneType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetNavigatedProperty(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return navigatedProperty.PropertyType.IsCollectionOfOneType();
        }

        private bool HasManyToManyRelationship(Type propertyType, Type entityType)
        {
            if (!propertyType.IsCollectionOfOneType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetNavigatedProperty(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return navigatedProperty.PropertyType.IsCollectionOfOneType();
        }

        // Get table for entity in a manner avoiding endless recursion
        private EntityTable GetEntityTable(Type entityType)
        {
            var table = _rootTables.FirstOrDefault(t => t.Type == entityType);
            
            if (table is null)
            {
                _rootTables.Add(this);
                table = new EntityTable(entityType, _rootTables);
                _rootTables.Remove(this);
            }

            return table;
        }

        private bool CalledByTable(Type type)
        {
            return _rootTables.Any(t => t.Type == type);
        }
    }
}