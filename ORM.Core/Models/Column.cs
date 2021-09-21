using System;
using System.Collections.Generic;
using System.Reflection;
using ORM.Core.Attributes;

namespace ORM.Core.Models
{
    public class Column
    {
        public string Name { get; private set; }
        
        public Type Type { get; private set; }

        public bool IsPrimaryKey { get; private set; }

        public bool IsUnique { get; private set; }

        public bool IsNullable { get; private set; }
        
        public bool IsForeignKey { get; private set; }

        public int? MaxLength { get; private set; }

        public Column(string name, Type type, bool isForeignKey = false, bool isNullable = true)
        {
            Name = name;
            Type = type;
            IsForeignKey = isForeignKey;
            IsNullable = isNullable;
        }
        
        public Column(PropertyInfo property)
        {
            Name = property.Name;
            Type = property.PropertyType;
            ReadAttributes(property);
        }

        private void ReadAttributes(PropertyInfo property)
        {
            IEnumerable<Attribute> attributes = property.GetCustomAttributes();

            foreach (Attribute attribute in attributes)
            {
                ReadAttribute(attribute);
            }
        }

        private void ReadAttribute(Attribute attribute)
        {
            if (attribute is PrimaryKeyAttribute)
            {
                IsPrimaryKey = true;
            }
                
            if (attribute is NotNullAttribute)
            {
                IsNullable = false;
            }
                
            if (attribute is UniqueAttribute)
            {
                IsUnique = true;
            }

            if (attribute is MaxLengthAttribute maxLengthAttribute)
            {
                MaxLength = maxLengthAttribute.Length;
            }
        }
    }
}