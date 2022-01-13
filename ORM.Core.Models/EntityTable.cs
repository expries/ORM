using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a table that holds information that is mapped to an entity
    /// </summary>
    public class EntityTable : Table
    {
        /// <summary>
        /// The type of entity stored in the table
        /// </summary>
        public Type Type { get; }
        
        /// <summary>
        /// Primary key column of the table
        /// </summary>
        public Column PrimaryKey { get; protected set; }

        /// <summary>
        /// List of looked up entities, used to avoid recursions when looking up relationships to other entities
        /// </summary>
        private static readonly List<EntityTable> EntityList = new List<EntityTable>();

        /// <summary>
        /// Create new table based on the given entity type
        /// </summary>
        /// <param name="entityType"></param>
        public EntityTable(Type entityType) : base(entityType.Name)
        {
            var proxiedType = entityType.GetProxiedType();
            
            if (proxiedType is not null)
            {
                entityType = proxiedType;
            }
            
            Type = entityType;
            ReadType(entityType);
        }
        
        /// <summary>
        /// Get the property of the entity type that corresponds to a given column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public PropertyInfo? GetPropertyForColumn(Column column)
        {
            var properties = Type.GetProperties();
            return properties.FirstOrDefault(p => new Column(p).Name == column.Name);
        }

        /// <summary>
        /// Get the properties that form a given relationship with the entity type 
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public IEnumerable<PropertyInfo> GetPropertiesOf(RelationshipType relationship)
        {
            var properties = Type.GetProperties();

            foreach (var property in properties)
            {
                var underlyingType = property.PropertyType.GetUnderlyingType();
                var field = ExternalFields.FirstOrDefault(f => 
                    f.Table.Type == underlyingType && 
                    f.Relationship == relationship
                );

                if (field is not null)
                {
                    yield return property;
                }
            }
        }

        /// <summary>
        /// Set table data based on given entity type
        /// </summary>
        /// <param name="entityType"></param>
        /// <exception cref="InvalidEntityException"></exception>
        private void ReadType(Type entityType)
        {
            if (entityType.IsInternalType())
            {
                throw new InvalidEntityException($"Type {entityType.Name} can not be represented by " +
                                                 $"a table. Please provide a complex type.");
            }

            var properties = entityType.GetProperties();
            var columnProperties = properties.Where(p => p.PropertyType.IsInternalType());

            // process properties which types can be converted into table columns
            foreach (var property in columnProperties)
            {
                AddColumn(property);
            }

            // check if entity has a mandatory primary key column
            try
            {
                var pk = Columns.SingleOrDefault(c => c.IsPrimaryKey);
                PrimaryKey = pk ?? throw new InvalidEntityException($"Type {Type.Name} is missing a primary key column.");
            }
            catch (InvalidOperationException)
            {
                throw new InvalidEntityException($"Type {Type.Name} is not allowed to have more than one primary key");
            }

            // process properties which are of an entity type
            var externalFieldProperties = properties.Where(p => p.PropertyType.IsExternalType());

            foreach (var property in externalFieldProperties)
            {
                ReadExternalField(property);
            }
        }

        /// <summary>
        /// Process property that is an entity itself
        /// </summary>
        /// <param name="property"></param>
        private void ReadExternalField(PropertyInfo property)
        {
            // generate the entity's table
            var type = property.PropertyType;
            var entityType = type.GetUnderlyingType();
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
        
        /// <summary>
        /// Add one-to-one relationship to other table
        /// </summary>
        /// <param name="other"></param>
        private void AddOneToOne(EntityTable other)
        {
            AddForeignKey(other, true);
            AddExternalField(other, RelationshipType.OneToOne);
        }

        /// <summary>
        /// Add one-to-many relationship to other table
        /// </summary>
        /// <param name="other"></param>
        private void AddOneToMany(EntityTable other)
        {
            AddExternalField(other, RelationshipType.OneToMany);
        }
        
        /// <summary>
        /// Add many-to-one relationship to other table
        /// </summary>
        /// <param name="other"></param>
        private void AddManyToOne(EntityTable other)
        {
            AddForeignKey(other, false);
            AddExternalField(other, RelationshipType.ManyToOne);
        }
        
        /// <summary>
        /// Add many-to-many relationship to other table
        /// </summary>
        /// <param name="other"></param>
        private void AddManyToMany(EntityTable other)
        {
            AddForeignKeyTable(other);
            AddExternalField(other, RelationshipType.ManyToMany);
            other.AddExternalField(this, RelationshipType.ManyToMany);
        }

        /// <summary>
        /// Add helper table to represent many-to-many relationship to other table
        /// </summary>
        /// <param name="other"></param>
        private void AddForeignKeyTable(EntityTable other)
        {
            var fkTable = new ForeignKeyTable(this, other);
            ForeignKeyTables.Add(fkTable);
        }

        /// <summary>
        /// Check if the relationship between two types is one-to-one
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEntityException"></exception>
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

        /// <summary>
        /// Check if the relationship between two types is one-to-many
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEntityException"></exception>
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
        
        /// <summary>
        /// Check if the relationship between two types is many-to-one
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEntityException"></exception>
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

        /// <summary>
        /// Check if the relationship between two types is many-to-many
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidEntityException"></exception>
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
        
        /// <summary>
        /// Lookup or create an entity table model for a given type
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private EntityTable GetEntityTable(Type entityType)
        {
            var table = EntityList.FirstOrDefault(t => t.Type == entityType);
            
            if (table is null)
            {
                EntityList.Add(this);
                table = new EntityTable(entityType);
                EntityList.Remove(this);
            }

            return table;
        }

        /// <summary>
        /// Check if the entity that is looked for, initiated the creation of the current instance of entity table 
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private static bool CalledByTable(Type entityType)
        {
            return EntityList.Any(t => t.Type == entityType);
        }
    }
}