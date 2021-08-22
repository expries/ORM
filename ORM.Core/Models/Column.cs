using System;
using System.Reflection;
using ORM.Core.DataTypes;
using ORM.Infrastructure.Attributes;
using ORM.Infrastructure.Exceptions;

namespace ORM.Core.Models
{
    public class Column
    {
        public string Name { get; set; }
        
        public IDbDataType Type { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool IsUnique { get; set; }

        public bool IsNullable { get; set; } = true;
        
        public bool IsForeignKey { get; set; }

        public Column(PropertyInfo property, IDbDataType type)
        {
            Name = property.Name;
            Type = type;
            ReadProperty(property);
        }

        public Column(string name, IDbDataType type)
        {
            Name = name;
            Type = type;
        }

        private void ReadProperty(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes();

            foreach (var attribute in attributes)
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
                if (Type is not IDbMaxLengthType lengthType)
                {
                    throw new InvalidAttributeUsageException(
                        $"Cannot apply attribute MaxLength on type '{Type.GetType().Name}'");
                }

                lengthType.Length = maxLengthAttribute.Length;
            }
        }
    }
}