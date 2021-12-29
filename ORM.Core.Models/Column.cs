using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using ORM.Core.Models.Attributes;

namespace ORM.Core.Models
{
    public class Column
    {
        public string Name { get; private set; }
        
        public Type Type { get; private set; }
        
        public bool IsMapped { get; private set; }

        public bool IsPrimaryKey { get; private set; }

        public bool IsUnique { get; private set; }

        public bool IsNullable { get; private set; }
        
        public bool IsForeignKey { get; private set; }

        public int? MaxLength { get; private set; }
        
        public int? MinLength { get; private set; }

        public Column(string name, Type type, bool isForeignKey = false, bool isNullable = true)
        {
            Name = name;
            Type = type;
            IsForeignKey = isForeignKey;
            IsNullable = isNullable;
            IsMapped = true;
        }
        
        public Column(PropertyInfo property)
        {
            Name = property.Name;
            Type = property.PropertyType;
            IsMapped = true;
            ReadAttributes(property);
        }

        public object? GetValue<T>(T entity)
        {
            var property = GetProperty(entity);
            object? value = property?.GetValue(entity);
            return value;
        }

        public void SetValue<T>(T entity, object value)
        {
            var property = GetProperty(entity);
            property?.SetValue(entity, value);
        }

        private void ReadAttributes(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes();

            foreach (var attribute in attributes)
            {
                ReadAttribute(attribute);
            }
        }

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

            if (attribute is MinLengthAttribute minLengthAttribute)
            {
                MinLength = minLengthAttribute.Length;
            }
        }
        
        private PropertyInfo? GetProperty<T>(T entity)
        {
            var properties = entity?.GetType().GetProperties();
            var property = properties?.FirstOrDefault(p => new Column(p).Name == Name);
            return property;
        }
    }
}