using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    }
}