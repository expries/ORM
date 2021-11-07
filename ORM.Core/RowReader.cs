using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using ORM.Core.Models;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Helpers;

namespace ORM.Core
{
    internal class RowReader<T> : IEnumerator<T>
    {
        private readonly IDataReader _reader;

        private Dictionary<int, string>? _schema;

        private bool _recordWasRead = false;

        private T _current;

        public T Current
        {
            get
            {
                if (!_recordWasRead)
                {
                    MoveNext();
                }
                
                return _current;
            }
        }

        object? IEnumerator.Current => Current;
        
        public RowReader(IDataReader reader)
        {
            _reader = reader;
        }
        
        public bool MoveNext()
        {
            _recordWasRead = true;
            bool recordWasFound = _reader.Read();

            if (!recordWasFound)
            {
                return false;
            }

            if (_schema is null)
            {
                ReadColumnSchema();
            }

            if (typeof(T).IsConvertibleToDbColumn())
            {
                ReadValueType();
                return true;
            }

            _current = Activator.CreateInstance<T>();
            ReadComplexType();
            return true;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        private void ReadColumnSchema()
        {
            _schema = new Dictionary<int, string>();
            int columnsCount = _reader.FieldCount;
            
            for (int i = 0; i < columnsCount; i++)
            {
                string columnName = _reader.GetName(i);
                _schema[i] = columnName;
            }
        }

        private void ReadValueType()
        {
            if (_schema.Count < 1)
            {
                throw new OrmException("Found result column for sql statement");
            }

            var valueType = _reader.GetFieldType(0);
            
            if (valueType == typeof(long) && typeof(T) == typeof(int))
            {
                _current = (T) (object) _reader.GetInt32(0);
                return;
            }
            
            object value = _reader.GetValue(0);
            _current = (T) value;
        }

        private void ReadComplexType()
        {
            var table = new EntityTable(typeof(T));
            var properties = typeof(T).GetProperties();
            var manyToOne = table.Relationships.Where(r => r.Type is TableRelationshipType.ManyToOne);
            var oneToMany = table.Relationships.Where(r => r.Type is TableRelationshipType.OneToMany);

            foreach (var property in properties)
            {
                var type = property.PropertyType;
                bool isManyToOne = manyToOne.Any(r => r.Table is EntityTable et && et.Type == type);
                bool isOneToMany = oneToMany.Any(r => r.Table is EntityTable et && et.Type == type);

                if (type.IsConvertibleToDbColumn())
                {
                    MapColumn(property);
                }

                type.IsCollectionOfAType();

                var entity = new EntityTable(type);
                
                if (isManyToOne)
                {
                    //MapManyToOne(property, table);
                    continue;
                }

                if (isOneToMany)
                {
                    //MapOneToMany(property, table);
                    continue;
                }
            }
        }

        private void MapColumn(PropertyInfo property)
        {
            var attribute = property.GetCustomAttribute(typeof(ColumnAttribute), true);
            string columnName = property.Name;

            if (attribute is ColumnAttribute columnAttribute)
            {
                columnName = columnAttribute.Name;
            }

            bool schemaMatchesProperty = _schema.Any(c => 
                string.Equals(c.Value, columnName, StringComparison.CurrentCultureIgnoreCase));
            
            if (!schemaMatchesProperty)
            {
                property.SetValue(_current, null);
                return;
            }

            var columnSchema = _schema.First(c => 
                string.Equals(c.Value, columnName, StringComparison.CurrentCultureIgnoreCase));
            
            int columnOrdinal = columnSchema.Key;
            object value = _reader.GetValue(columnOrdinal);

            if (value is DBNull)
            {
                property.SetValue(_current, null);
                return;
            }
            
            property.SetValue(_current, value);
        }
    }
}