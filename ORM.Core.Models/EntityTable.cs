using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
        private void AddOneToOne(Table other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, true);
            AddRelationship(other, RelationshipType.OneToOne);
        }

        // Add one-to-many relationship to other table
        private void AddOneToMany(Table other)
        {
            AddRelationship(other, RelationshipType.OneToMany);
        }
        
        // Add many-to-one relationship to other table
        private void AddManyToOne(Table other)
        {
            string fkName = $"fk_{other.Name}";
            AddForeignKey(fkName, other, false);
            AddRelationship(other, RelationshipType.ManyToOne);
        }
        
        // Add many-to-many relationship to other table
        private void AddManyToMany(Table other)
        {
            var fkTable = CreateManyToManyTable(other);
            AddRelationship(fkTable, RelationshipType.OneToMany);
            AddRelationship(other, RelationshipType.ManyToMany);
            other.AddRelationship(fkTable, RelationshipType.OneToMany);
            other.AddRelationship(this, RelationshipType.ManyToMany);
        }

        // Create helper table to represent many-to-many relationship to other table
        private Table CreateManyToManyTable(Table other)
        {
            bool firstType = Type.GUID.GetHashCode() > (other as EntityTable)?.Type.GUID.GetHashCode();
            Column pkThis;
            Column pkOther;
            
            if (firstType)
            {
                pkThis = Columns.First(c => c.IsPrimaryKey);
                pkOther = other.Columns.First(c => c.IsPrimaryKey);
                return new ForeignKeyTable(pkThis, pkOther, this, other);
            }

            pkThis = other.Columns.First(c => c.IsPrimaryKey);
            pkOther = Columns.First(c => c.IsPrimaryKey);
            return new ForeignKeyTable(pkThis, pkOther, other, this);
        }
        
        private bool HasOneToOneRelationship(Type propertyType, Type entityType)
        {
            if (propertyType.IsCollectionOfAType())
            {
                return false;
            }
            
            var navigatedProperty = propertyType.GetPropertyOfTypeFirstOrDefault(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return !navigatedProperty.PropertyType.IsCollectionOfAType();
        }

        private bool HasOneToManyRelationship(Type propertyType, Type entityType)
        {
            if (!propertyType.IsCollectionOfAType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetPropertyOfTypeFirstOrDefault(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return !navigatedProperty.PropertyType.IsCollectionOfAType();
        }
        
        private bool HasManyToOneRelationship(Type propertyType, Type entityType)
        {
            if (propertyType.IsCollectionOfAType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetPropertyOfTypeFirstOrDefault(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return navigatedProperty.PropertyType.IsCollectionOfAType();
        }

        private bool HasManyToManyRelationship(Type propertyType, Type entityType)
        {
            if (!propertyType.IsCollectionOfAType())
            {
                return false;
            }
            
            var navigatedProperty = entityType.GetPropertyOfTypeFirstOrDefault(Type);

            if (navigatedProperty is null)
            {
                throw new InvalidEntityException($"Entity {entityType.Name} does not have navigated property " +
                                                 $"for type {Type.Name}, can't resolve relationship.");
            }
            
            return navigatedProperty.PropertyType.IsCollectionOfAType();
        }

        // Get table for entity in a manner avoiding endless recursion
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

        private bool CalledByTable(Type type)
        {
            return _rootTables.Any(t => t.Type == type);
        }
    }
}