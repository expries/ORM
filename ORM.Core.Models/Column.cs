using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Attributes;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Models
{
    /// <summary>
    /// Represents a database column
    /// </summary>
    public class Column
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Type stored in this column
        /// </summary>
        public Type Type { get; private set; }
        
        /// <summary>
        /// The remote table for foreign key columns
        /// </summary>
        public Table? Table { get; private set; }

        /// <summary>
        /// If this column should be mapped to/from the database
        /// </summary>
        public bool IsMapped { get; private set; }

        /// <summary>
        /// If this column is a primary key
        /// </summary>
        public bool IsPrimaryKey { get; private set; }

        /// <summary>
        /// If column has a unique constraint
        /// </summary>
        public bool IsUnique { get; private set; }

        /// <summary>
        /// If this column may be null
        /// </summary>
        public bool IsNullable { get; private set; }
        
        /// <summary>
        /// If this column is a foreign key
        /// </summary>
        public bool IsForeignKey { get; private set; }

        /// <summary>
        /// The maximum length that may be stored in this column
        /// </summary>
        public int? MaxLength { get; private set; }

        public Column(string name, Type type, Table table, bool isForeignKey = false, bool isNullable = true)
        {
            Name = name;
            Type = type;
            IsForeignKey = isForeignKey;
            IsNullable = isNullable;
            Table = table;
            IsMapped = true;
        }
        
        public Column(PropertyInfo property)
        {
            Name = property.Name;
            Type = property.PropertyType;
            IsMapped = true;
            ReadAttributes(property);
        }

        /// <summary>
        /// Gets the value a given entity stores in this column
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public object? GetValue<T>(T entity)
        {
            // for columns that are not foreign keys, return the properties.
            if (!IsForeignKey)
            {
                var property = GetProperty(entity);
                object? value = property?.GetValue(entity);
                return value;    
            }

            if (Table is not EntityTable remoteEntityTable)
            {
                return null;
            }

            // Get the reference entity
            var entityType = entity?.GetType();
            var propertyForForeignKey = entityType?
                .GetProperties()
                .First(x => x.PropertyType.GetUnderlyingType() == remoteEntityTable.Type);
            
            object? referencedEntity = propertyForForeignKey?.GetValue(entity);
            // return the reference entity's primary key
            return remoteEntityTable.PrimaryKey.GetValue(referencedEntity);
        }

        /// <summary>
        /// Sets a value for this column on a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public void SetValue<T>(T entity, object value)
        {
            var property = GetProperty(entity);
            property?.SetValue(entity, value);
        }

        /// <summary>
        /// Sets the column data based on a given property
        /// </summary>
        /// <param name="property"></param>
        private void ReadAttributes(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes();

            foreach (var attribute in attributes)
            {
                ReadAttribute(attribute);
            }
        }

        /// <summary>
        /// Sets the column data based on a given attribute
        /// </summary>
        /// <param name="attribute"></param>
        private void ReadAttribute(Attribute attribute)
        {
            if (attribute is ColumnAttribute columnAttribute)
            {
                Name = string.IsNullOrEmpty(columnAttribute.Name) ? Name : columnAttribute.Name;
            }
            
            if (attribute is KeyAttribute)
            {
                IsPrimaryKey = true;
            }
                
            if (attribute is RequiredAttribute)
            {
                IsNullable = false;
            }
                
            if (attribute is UniqueAttribute)
            {
                IsUnique = true;
            }

            if (attribute is NotMappedAttribute)
            {
                IsMapped = false;
            }

            if (attribute is MaxLengthAttribute maxLengthAttribute)
            {
                MaxLength = maxLengthAttribute.Length;
            }
        }
        
        /// <summary>
        /// Gets the property for this column from an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private PropertyInfo? GetProperty<T>(T entity)
        {
            var properties = entity?.GetType().GetProperties();
            var property = properties?.FirstOrDefault(p => new Column(p).Name == Name);
            return property;
        }
    }
}