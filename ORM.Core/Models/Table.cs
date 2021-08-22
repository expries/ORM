using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Helpers;
using ORM.Core.SqlDialects;
using ORM.Infrastructure.Exceptions;

namespace ORM.Core.Models
{
    public class Table
    {
        private readonly IDbTypeMapper _typeMapper;

        private readonly Table _relatedTable;
        
        public string Name { get; set; }
        
        public Type ObjectType { get; }
        
        public List<Column> Columns { get; } = new List<Column>();

        public List<ForeignKeyConstraint> ForeignKeys { get; } = new List<ForeignKeyConstraint>();

        public Dictionary<Type, TableRelationship> Relationships { get; set; } = 
            new Dictionary<Type, TableRelationship>();

        public Table(IDbTypeMapper typeMapper, Type entityType)
        {
            _typeMapper = typeMapper;
            ObjectType = entityType;
            Name = entityType.Name;
            ReadType(entityType);
        }
        
        private Table(IDbTypeMapper typeMapper, Type entityType, Table relatedTable)
        {
            _typeMapper = typeMapper;
            _relatedTable = relatedTable;
            ObjectType = entityType;
            Name = entityType.Name;
            ReadType(entityType);
        }

        private void ReadType(Type entityType)
        {
            var properties = entityType.GetProperties();
            
            foreach (var property in properties)
            {
                ReadProperty(property);
            }
        }

        private void ReadProperty(PropertyInfo property)
        {
            var type = property.PropertyType;
            
            // type that maps to postgres data type
            if (type.IsSimpleType())
            {
                var dbType = _typeMapper.Map(type);
                var column = new Column(property, dbType);
                Columns.Add(column);
                return;
            }

            // type maps to "many to ..." relationship
            if (type.IsCollectionOfAType())
            {
                var otherTable = _relatedTable;
                var otherType = type.GetGenericArguments().First();
                    
                if (otherType.IsSimpleType())
                {
                    throw new InvalidEntityException("Can't map enumerable of value type to database");
                }
                
                if (otherTable is null || otherType != otherTable.ObjectType)
                {
                    ResolveManyToRelationship(otherType);
                    return;
                }

                if (otherTable.Relationships[ObjectType].Type is TableRelationshipType.OneToMany)
                {
                    ResolveManyToOneRelationship(otherTable, otherType);
                    return;
                }
                
                var relationship = new TableRelationship
                {
                    Table = otherTable,
                    Type = TableRelationshipType.ManyToMany
                };
            
                Relationships[otherType] = relationship;
                
                string fkOneName = $"fk_{property.Name}";
                string fkTwoName = $"fk_{ObjectType.Name}";
                AddManyToManyForeignKey(fkOneName, fkTwoName, otherTable);
                return;
            }

            if (_relatedTable?.ObjectType == type && 
                _relatedTable?.Relationships[ObjectType].Type is TableRelationshipType.OneToMany)
            {
                var relationshipOneToOne = new TableRelationship
                {
                    Table = _relatedTable,
                    Type = TableRelationshipType.OneToOne
                };

                Relationships[type] = relationshipOneToOne;
                AddOneToOneForeignKey(property.Name, _relatedTable);
                return;
            }
            
            // type maps to "one to many" relationship
            var relationshipX = new TableRelationship
            {
                Table = null,
                Type = TableRelationshipType.OneToMany
            };
            
            Relationships[type] = relationshipX;
            var relatedTable = new Table(_typeMapper, type, this);
            relationshipX.Table = relatedTable;

            if (!relatedTable.Relationships.ContainsKey(ObjectType) || 
                relatedTable.Relationships[ObjectType].Type is TableRelationshipType.OneToOne)
            {
                relationshipX.Type = TableRelationshipType.OneToOne;
            }
            
            AddOneToManyForeignKey(property.Name, relatedTable);
        }

        private void ResolveManyToRelationship(Type otherType)
        {
            var relationship = new TableRelationship
            {
                Table = null, 
                Type = TableRelationshipType.ManyToMany
            };
            
            Relationships[otherType] = relationship;

            var table = new Table(_typeMapper, otherType, this);

            if (!table.Relationships.ContainsKey(ObjectType))
            {
                throw new InvalidEntityException($"Type '{otherType.Name}' has no property of type " +
                                                 $"'{ObjectType.Name}' to resolve the many-to-* relationship.");
            }

            if (table.Relationships[ObjectType].Type is TableRelationshipType.OneToMany)
            {
                relationship.Type = TableRelationshipType.ManyToOne;
            }
            
            relationship.Table = table;
        }

        private void ResolveManyToOneRelationship(Table otherTable, Type otherType)
        {
            Relationships[otherType] = new TableRelationship
            {
                Table = otherTable,
                Type = TableRelationshipType.ManyToOne
            };
        }

        private void AddManyToManyForeignKey(string fkOneName, string fkTwoName, Table otherTable)
        {
            // create many-to-many table
            var fkTable = new Table(_typeMapper, typeof(ForeignKeyTable))
            {
                Name = $"{Name}_{otherTable.Name}"
            };

            Relationships[typeof(ForeignKeyTable)] = new TableRelationship
            {
                Table = fkTable,
                Type = TableRelationshipType.OneToMany
            };
            
            otherTable.Relationships[typeof(ForeignKeyTable)] = new TableRelationship
            {
                Table = fkTable,
                Type = TableRelationshipType.OneToMany
            };

            // add foreign keys constraints
            var fkThisTable = new ForeignKeyConstraint
            {
                ColumnFrom = fkTable.Columns[0],
                ColumnTo = Columns.First(c => c.IsPrimaryKey),
                TableTo = this,
            };
                    
            var fkOtherTable = new ForeignKeyConstraint
            {
                ColumnFrom = fkTable.Columns[1],
                ColumnTo = otherTable.Columns.First(c => c.IsPrimaryKey),
                TableTo = otherTable,
            };
            
            fkTable.ForeignKeys.Add(fkThisTable);
            fkTable.ForeignKeys.Add(fkOtherTable);
        }

        private void AddOneToManyForeignKey(string fkName, Table otherTable)
        {
            var dbIntType = _typeMapper.Map(typeof(int));
            var fkColumn = new Column($"fk_{fkName}", dbIntType) { IsForeignKey = true, IsNullable = false };

            var fk = new ForeignKeyConstraint
            {
                ColumnFrom = fkColumn,
                ColumnTo = otherTable.Columns.First(x => x.IsPrimaryKey), 
                TableTo = otherTable
            };
            
            Columns.Add(fkColumn);
            ForeignKeys.Add(fk);
        }
        
        private void AddOneToOneForeignKey(string fkName, Table otherTable)
        {
            var dbIntType = _typeMapper.Map(typeof(int));
            var fkColumn = new Column($"fk_{fkName}", dbIntType) { IsForeignKey = true };

            var fk = new ForeignKeyConstraint
            {
                ColumnFrom = fkColumn,
                ColumnTo = otherTable.Columns.First(x => x.IsPrimaryKey), 
                TableTo = otherTable
            };
            
            Columns.Add(fkColumn);
            ForeignKeys.Add(fk);
        }
    }
}