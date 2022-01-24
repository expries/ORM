using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Exceptions;
using ORM.Core.Models.Extensions;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresCommandBuilder : ICommandBuilder
    {
        private readonly IDbTypeMapper _typeMapper;
        
        private readonly StringBuilder _sql = new StringBuilder();

        private readonly IDbConnection _connection;

        private int _parameterCount;

        public PostgresCommandBuilder(IDbConnection connection, IDbTypeMapper typeMapper)
        {
            _connection = connection;
            _typeMapper = typeMapper;
        }

        /// <summary>
        /// Builds a commands to create the given list of tables in a database
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDbCommand BuildEnsureCreated(List<Table> tables)
        {
            tables.ForEach(WriteDropTable);
            tables.ForEach(WriteCreateTable);
            
            foreach (var table in tables)
            {
                table.ForeignKeys.ForEach(fc => WriteAddForeignKey(fc, table));
            }

            var query = CreateQuery();
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to get all entities of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildGetAll<T>()
        {
            var table = typeof(T).ToTable();
            
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");
            
            string columnsString = string.Join(",", columns);
            
            _sql
                .Append($"SELECT {columnsString}")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"FROM \"{table.Name}\"");
            
            var query = CreateQuery();
            return CreateCommand(query);
        }
        
        /// <summary>
        /// Builds a command ot get an entity of type T by its primary key 
        /// </summary>
        /// <param name="pk"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildGetById<T>(object pk)
        {
            var table = typeof(T).ToTable();
            var pkParameter = CreateParameter(pk);
            
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");
            
            string columnsString = string.Join(",", columns);
            _sql.Append($"SELECT {columnsString}")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"FROM \"{table.Name}\"")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"WHERE \"{table.PrimaryKey.Name}\" = @{pkParameter.Name}");

            var query = CreateQuery(pkParameter);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to save an entity to the database
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildSave<T>(T entity)
        {
            if (entity is null)
            {
                throw new OrmException("Can't save entity that is set to null.");
            }
            
            var type = entity.GetType();
            var table = type.ToTable();
            
            var parameters = new List<QueryParameter>();
            var columnsForParameters = new Dictionary<QueryParameter, Column>();
            
            // Get parameters for internal fields 
            foreach (var column in table.Columns)
            {
                if (column.IsMapped && !column.IsForeignKey)
                {
                    object? value = column.GetValue(entity);
                    var parameter = CreateParameter(value);
                    columnsForParameters[parameter] = column;
                    parameters.Add(parameter);
                }
            }
            
            var manyToOne = table.GetPropertiesOf(RelationshipType.ManyToOne);
            
            // Add foreign key parameters
            foreach (var property in manyToOne)
            {
                var propertyType = property.PropertyType.GetUnderlyingType();
                var foreignKey = table.ForeignKeys.First(fk => fk.RemoteTable.Type == propertyType);

                object? value = property.GetValue(entity);
                object? primaryKey = GetPrimaryKey(value);
                
                var fkParameter = CreateParameter(primaryKey);
                columnsForParameters[fkParameter] = foreignKey.LocalColumn;
                parameters.Add(fkParameter);
            }

            // Create insert/update statement
            var columns = columnsForParameters.Select(p => $"\"{p.Value.Name}\"");
            var values = parameters.Select(p => $"@{p.Name}");
            string columnsString = string.Join(',', columns);
            string valuesString = string.Join(',', values);

            _sql
                .Append($"INSERT INTO \"{table.Name}\" ({columnsString})")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"VALUES ({valuesString})")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"ON CONFLICT (\"{table.PrimaryKey.Name}\") DO")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append("UPDATE")
                .Append(' ')
                .Append("SET")
                .Append(' ')
                .Append(Environment.NewLine);
            
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var column = columnsForParameters[parameter];
                
                _sql.Append($"\"{column.Name}\" = @{parameter.Name}");

                if (i < parameters.Count - 1)
                {
                    _sql.Append(',');
                }
                
                _sql.Append(Environment.NewLine);
            }

            _sql
                .Append($"RETURNING \"{table.PrimaryKey.Name}\"")
                .Append(Environment.NewLine);

            var query = CreateQuery(parameters);
            return CreateCommand(query);
        }

        public IDbCommand BuildDeleteById<T>(object pk)
        {
            var entityType = typeof(T);
            var entityTable = entityType.ToTable();
            var primaryKeyParameter = CreateParameter(pk);

            _sql
                .Append($"DELETE FROM \"{entityTable.Name}\" ")
                .Append(Environment.NewLine)
                .Append($"WHERE {entityTable.PrimaryKey.Name} = @{primaryKeyParameter.Name}");

            var query = CreateQuery(primaryKeyParameter);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to remove references between an entity and a reference type that share a
        /// many to many relationship by deleting rows in their corresponding joining table.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="referenceType"></param>
        /// <returns></returns>
        public IDbCommand BuildRemoveManyToManyReferences(object entity, Type referenceType)
        {
            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();
            var referenceTable = referenceType.ToTable();
            
            // get foreign key helper table
            var fkTable = entityTable.ForeignKeyTables.First(x => 
                x.MapsTypes(entityTable.Type, referenceTable.Type));
            
            // get foreign key that points to entity types table
            var fkColumnForEntityTable = fkTable.ForeignKeys
                .First(fk => fk.RemoteTable.Name == entityTable.Name)
                .LocalColumn;
            
            object? primaryKey = entityTable.PrimaryKey.GetValue(entity);
            var primaryKeyParameter = CreateParameter(primaryKey);

            _sql
                .Append($"DELETE FROM \"{fkTable.Name}\"")
                .Append(' ')
                .Append($"WHERE \"{fkColumnForEntityTable.Name}\" = @{primaryKeyParameter.Name}")
                .Append(' ')
                .Append($"{Environment.NewLine}");

            var query = CreateQuery(primaryKeyParameter);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to save the references between two entities that share a many to many
        /// relationship by inserting rows in the corresponding joining table 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="referenceType"></param>
        /// <param name="referencePrimaryKeys"></param>
        /// <returns></returns>
        public IDbCommand BuildSaveManyToManyReferences(object entity, Type referenceType, List<object> referencePrimaryKeys)
        {
            var entityType = entity.GetType();
            var entityTable = entityType.ToTable();
            var referenceTable = referenceType.ToTable();
            var parameters = new List<QueryParameter>();

            // get foreign key helper table
            var fkTable = entityTable.ForeignKeyTables.First(x => x.MapsTypes(entityTable.Type, referenceTable.Type));

            // get columns for foreign keys
            var sourceColumn = fkTable.ForeignKeys.First(fk => fk.RemoteTable.Type == entityTable.Type).LocalColumn;
            var destColumn = fkTable.ForeignKeys.First(fk => fk.RemoteTable.Name == referenceTable.Name).LocalColumn;

            // Insert new reference rows into many to many table
            object? primaryKey = entityTable.PrimaryKey.GetValue(entity);
            var primaryKeyParameter = CreateParameter(primaryKey);
            parameters.Add(primaryKeyParameter);

            _sql
                .Append($"INSERT INTO \"{fkTable.Name}\" (\"{sourceColumn.Name}\", \"{destColumn.Name}\")")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append(' ')
                .Append("VALUES")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append(' ');

            for (int i = 0; i < referencePrimaryKeys.Count; i++)
            {
                object referencePk = referencePrimaryKeys[i];
                var referencePkParameter = CreateParameter(referencePk);
                parameters.Add(referencePkParameter);
                
                _sql.Append($"(@{primaryKeyParameter.Name}, @{referencePkParameter.Name})");

                if (i < referencePrimaryKeys.Count - 1)
                {
                    _sql.Append(',');
                }

                _sql.Append(Environment.NewLine);
            }
            
            var query = CreateQuery(parameters);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to load an entity that is the one-side of a many-to-one relationship
        /// given the entity of the many side
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TMany"></typeparam>
        /// <typeparam name="TOne"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadManyToOne<TMany, TOne>(TMany entity)
        {
            var manyTable = typeof(TMany).ToTable();
            var oneTable = typeof(TOne).ToTable();
            
            object? pk = manyTable.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                throw new OrmException($"Failed to load many to one relationship: no primary key " +
                                       $"found for type {manyTable.Type.Name}");
            }
            
            var pkParameter = CreateParameter(pk);
            
            var columns = oneTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"t.\"{c.Name}\"");

            string columnString = string.Join(',', columns);
            var fk  = manyTable.ForeignKeys.First(c => c.RemoteTable.Type == oneTable.Type);

            _sql
                .Append($"SELECT {columnString}")
                .Append(' ')
                .Append($"FROM \"{manyTable.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{oneTable.Name}\" t")
                .Append(' ')
                .Append($"ON t.\"{oneTable.PrimaryKey.Name}\" = \"{manyTable.Name}\".\"{fk.LocalColumn.Name}\"")
                .Append(' ')
                .Append($"WHERE \"{manyTable.Name}\".\"{manyTable.PrimaryKey.Name}\" = @{pkParameter.Name}");

            var query = CreateQuery(pkParameter);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to load the entities that are the many-side of a one-to-many relationship given
        /// an entity of the one-side
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TOne"></typeparam>
        /// <typeparam name="TMany"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadOneToMany<TOne, TMany>(TOne entity)
        {
            var oneTable = typeof(TOne).ToTable();
            var manyTable = typeof(TMany).ToTable();
            object? pk = oneTable.PrimaryKey.GetValue(entity);

            if (pk is null)
            {
                throw new OrmException($"Failed to load many to one relationship: no primary key " +
                                       $"found for type {manyTable.Type.Name}");
            }
            
            var pkParameter = CreateParameter(pk);
            
            var columns = manyTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(',', columns);
            var fk  = manyTable.ForeignKeys.First(c => c.RemoteTable.Type == oneTable.Type);

            _sql
                .Append($"SELECT {columnsString}")
                .Append(' ')
                .Append($"FROM \"{manyTable.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{oneTable.Name}\" t")
                .Append(' ')
                .Append($"ON t.\"{oneTable.PrimaryKey.Name}\" = \"{manyTable.Name}\".\"{fk.LocalColumn.Name}\"")
                .Append(' ')
                .Append($"WHERE t.\"{oneTable.PrimaryKey.Name}\" = @{pkParameter.Name}");
            
            var query = CreateQuery(pkParameter);
            return CreateCommand(query);
        }

        /// <summary>
        /// Builds a command to load the entities of a related type in a many-to-many relationship, given an
        /// entity of the other side of the relationship
        /// </summary>
        /// <param name="entity"></param>
        /// <typeparam name="TManyA"></typeparam>
        /// <typeparam name="TManyB"></typeparam>
        /// <returns></returns>
        public IDbCommand BuildLoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var manyATable = typeof(TManyA).ToTable();
            var manyBTable = typeof(TManyB).ToTable();

            // Find foreign key table for the two types
            var fkTable = manyATable.ForeignKeyTables.First(_ => _.MapsTypes(manyATable.Type, manyBTable.Type));

            // Find tables for entities
            var fkTableA = fkTable.ForeignKeys.First(fk => fk.RemoteTable.Type == manyATable.Type);
            var fkTableB = fkTable.ForeignKeys.First(fk => fk.RemoteTable.Type == manyBTable.Type);
            
            // Build SQL
            var columns = manyBTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyBTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(",", columns);

            _sql.Append($"SELECT {columnsString}")
                .Append(' ')
                .Append($"FROM \"{manyATable.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{fkTable.Name}\" t")
                .Append(' ')
                .Append($"ON \"{manyATable.Name}\".\"{manyATable.PrimaryKey.Name}\" = t.\"{fkTableA.LocalColumn.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{manyBTable.Name}\"")
                .Append(' ')
                .Append($"ON t.\"{fkTableB.LocalColumn.Name}\" = \"{manyBTable.Name}\".\"{manyBTable.PrimaryKey.Name}\"");

            var query = CreateQuery();
            return CreateCommand(query);
        }

        private void WriteDropTable(Table table)
        {
            _sql
                .Append($"DROP TABLE IF EXISTS \"{table.Name}\" CASCADE;")
                .Append(Environment.NewLine);
        }
        
        private void WriteCreateTable(Table table)
        {
            _sql
                .Append($"CREATE TABLE \"{table.Name}\"")
                .Append(' ')
                .Append('(')
                .Append(Environment.NewLine);

            int i = 0;
            var columns = table.Columns.Where(c => c.IsMapped);
            
            foreach (var column in columns)
            {
                _sql.Append('\t');
                WriteColumn(column);
                
                if (i < table.Columns.Count - 1)
                {
                    _sql.Append(',');
                }

                _sql.Append(Environment.NewLine);
                i++;
            }

            _sql
                .Append(')')
                .Append(';')
                .Append(Environment.NewLine);
        }
        
        private void WriteColumn(Column column)
        {
            var dbType = _typeMapper.Map(column.Type);
            
            if (column.MaxLength.HasValue && dbType is IDbMaxLengthDbType maxLengthType)
            {
                maxLengthType.Length = column.MaxLength.Value;
                dbType = maxLengthType;
            }

            _sql
                .Append($"\"{column.Name}\"")
                .Append(' ')
                .Append(dbType);

            if (column.IsPrimaryKey)
            {
                _sql
                    .Append(' ')
                    .Append("PRIMARY KEY");
            }
                
            if (!column.IsNullable)
            {
                _sql
                    .Append(' ')
                    .Append("NOT NULL");
            }

            if (column.IsUnique)
            {
                _sql
                    .Append(' ')
                    .Append("UNIQUE");
            }
        }
        
        private void WriteAddForeignKey(ForeignKey foreignKey, Table table)
        {
            _sql
                .Append("ALTER TABLE")
                .Append(' ')
                .Append($"\"{table.Name}\"")
                .Append(' ')
                .Append("ADD CONSTRAINT")
                .Append(' ')
                .Append($"\"{foreignKey.LocalColumn.Name}_{foreignKey.RemoteColumn.Name}\"")
                .Append(' ')
                .Append("FOREIGN KEY")
                .Append('(')
                .Append($"\"{foreignKey.LocalColumn.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("REFERENCES")
                .Append(' ')
                .Append($"\"{foreignKey.RemoteTable.Name}\"")
                .Append('(')
                .Append($"\"{foreignKey.RemoteColumn.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("ON DELETE CASCADE")
                .Append(';')
                .Append(Environment.NewLine);
        }

        /// <summary>
        /// Creates an IDbCommand for a given query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private IDbCommand CreateCommand(Query query)
        {
            // create command and set sql
            var cmd = _connection.CreateCommand();
            cmd.CommandText = query.Sql;
            
            // add parameters to command
            query.Parameters
                .ToList()
                .ForEach(p => cmd.AddParameter(p.Name, p.Value));

            // reset parameter count, that new commands will start counting at 0
            _parameterCount = 0;
            
            Console.WriteLine($"Executing SQL statement: {Environment.NewLine}{cmd.CommandText}{Environment.NewLine}");
            return cmd;
        }
        
        /// <summary>
        /// Creates a query consisting of the SQL written to the string-builder and the given parameters.
        /// Empties the SQL string builder.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private Query CreateQuery(params QueryParameter[] parameters)
        {
            var parameterList = parameters.ToList();
            return CreateQuery(parameterList);
        }

        /// <summary>
        /// Creates a query consisting of the SQL written to the string-builder and the given parameters.
        /// Empties the SQL string builder.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private Query CreateQuery(List<QueryParameter> parameters)
        {
            string sql = _sql.ToString();
            var query = new Query(sql, parameters);
            _sql.Clear();
            return query;
        }

        /// <summary>
        /// Create a parameter for a value.
        /// The parameter's name is automatically assigned.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private QueryParameter CreateParameter(object? value)
        {
            string parameterName = $"p{++_parameterCount}";
            return new QueryParameter(parameterName, value);
        }
        
        /// <summary>
        /// Gets the primary key of an entity
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static object? GetPrimaryKey(object? value)
        {
            var type = value?.GetType();
            var table = type?.ToTable();
            var pkColumn = table?.PrimaryKey;
            return pkColumn?.GetValue(value);
        }
    }
}