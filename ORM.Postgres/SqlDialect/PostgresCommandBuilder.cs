using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Npgsql;
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
        
        private IDbConnection Connection { get; }
        
        private StringBuilder _sb = new StringBuilder();

        private int _parameterCount;

        public PostgresCommandBuilder(NpgsqlConnection connection, IDbTypeMapper typeMapper)
        {
            Connection = connection;
            _typeMapper = typeMapper;
        }

        /// <summary>
        /// Builds a commands to create the given list of tables in a database
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public IDbCommand BuildEnsureCreated(List<Table> tables)
        {
            tables = tables.ToList();
            var createTables = TranslateCreateTables(tables);
            var dropTables = TranslateDropTables(tables);
            var addForeignKeys = TranslateAddForeignKeys(tables);

            _sb.Append(dropTables.Sql);
            _sb.Append(createTables.Sql);
            _sb.Append(addForeignKeys.Sql);
            
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
            var query = TranslateSelectAll<T>();
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
            var query = TranslateSelectById<T>(pk);
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
            var query = TranslateSaveEntity(entity);
            return CreateCommand(query);
        }

        public IDbCommand BuildDeleteById<T>(object pk)
        {
            var query = TranslateDelete<T>(pk);
            return CreateCommand(query);
        }

        private Query TranslateDelete<T>(object pk)
        {
            var entityType = typeof(T);
            var entityTable = entityType.ToTable();
            var primaryKeyParameter = Parameterize("pk", pk);

            _sb
                .Append($"DELETE FROM \"{entityTable.Name}\" ")
                .Append(Environment.NewLine)
                .Append($"WHERE {entityTable.PrimaryKey.Name} = @{primaryKeyParameter.Name}");

            return CreateQuery(primaryKeyParameter);
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
            var primaryKey = entityTable.PrimaryKey.GetValue(entity);
            var referenceTable = referenceType.ToTable();
            
            // get foreign key helper table
            var fkTable = entityTable.ForeignKeyTables.First(x => 
                x.MapsTypes(entityTable.Type, referenceTable.Type));
            
            // get foreign key that points to entity types table
            var fkColumnForEntityTable = fkTable.ForeignKeys
                .First(fk => fk.RemoteTable.Name == entityTable.Name)
                .LocalColumn;
            
            var cmd = Connection.CreateCommand();
            cmd.AddParameter("pk", primaryKey ?? DBNull.Value);
            
            cmd.CommandText = $"DELETE FROM \"{fkTable.Name}\" " +
                              $"WHERE \"{fkColumnForEntityTable.Name}\" = @pk " +
                              $"{Environment.NewLine}";

            return cmd;
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

            var primaryKey = entityTable.PrimaryKey.GetValue(entity);

            // get foreign key helper table
            var fkTable = entityTable.ForeignKeyTables.First(x => 
                x.MapsTypes(entityTable.Type, referenceTable.Type));

            // get columns for foreign keys
            var sourceColumn = fkTable.ForeignKeys
                .First(fk => fk.RemoteTable.Name == entityTable.Name)
                .LocalColumn;
            
            var destColumn = fkTable.ForeignKeys
                .First(fk => fk.RemoteTable.Name == referenceTable.Name)
                .LocalColumn;

            // Insert new reference rows into many to many table
            var subCmd = Connection.CreateCommand();
            
            subCmd.AddParameter("pk", primaryKey ?? DBNull.Value);

            subCmd.CommandText = $"INSERT INTO \"{fkTable.Name}\" (\"{sourceColumn.Name}\", \"{destColumn.Name}\") " +
                                 $"{Environment.NewLine}" + 
                                 $"VALUES " +
                                 $"{Environment.NewLine} ";

            for (int i = 0; i < referencePrimaryKeys.Count; i++)
            {
                object destinationPk = referencePrimaryKeys[i];
                subCmd.AddParameter($"pkDest{i}", destinationPk);
                subCmd.CommandText += $"(@pk, @pkDest{i})";

                if (i < referencePrimaryKeys.Count - 1)
                {
                    subCmd.CommandText += ",";
                }

                subCmd.CommandText += Environment.NewLine;
            }

            return subCmd;
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
            var query = TranslateLoadManyToOne<TMany, TOne>(entity);
            return CreateCommand(query, newConnection: true);
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
            var query = TranslateLoadOneToMany<TOne, TMany>(entity);
            return CreateCommand(query, newConnection: true);
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
            var query = TranslateLoadManyToMany<TManyA, TManyB>(entity);
            return CreateCommand(query, newConnection: true);
        }
        
        private Query CreateQuery(params QueryParameter[] parameters)
        {
            var parameterList = parameters.ToList();
            return CreateQuery(parameterList);
        }

        private Query CreateQuery(List<QueryParameter> parameters)
        {
            string sql = _sb.ToString();
            var query = new Query(sql, parameters);
            _sb.Clear();
            return query;
        }
        
        private static NpgsqlConnection CreateConnection()
        {
            var connection = new NpgsqlConnection("Server=localhost;Port=5434;User Id=postgres;Password=postgres;");
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Creates an IDbCommand given a query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="newConnection"></param>
        /// <returns></returns>
        private IDbCommand CreateCommand(Query query, bool newConnection = false)
        {
            var connection = newConnection ? CreateConnection() : Connection;
            
            // create command and set sql
            var cmd = connection.CreateCommand();
            cmd.CommandText = query.Sql;
            
            // add parameters to command
            query.Parameters
                .ToList()
                .ForEach(p => cmd.AddParameter(p.Name, p.Value));

            // reset parameter count, that new commands will start counting at 0
            _parameterCount = 0;

            Console.WriteLine(cmd.CommandText);
            return cmd;
        }
        
        private Query TranslateCreateTables(List<Table> tables)
        {
            var queries = tables.Select(TranslateCreateTable).ToList();
            queries.ForEach(x => _sb.Append(x.Sql));
            return CreateQuery();
        }

        private Query TranslateDropTables(List<Table> tables)
        {
            var queries = tables.Select(TranslateDropTable).ToList();
            queries.ForEach(x => _sb.Append(x.Sql));
            return CreateQuery();
        }
        
        private Query TranslateAddForeignKeys(List<Table> tables)
        {
            _sb = new StringBuilder();
            
            foreach (var table in tables)
            {
                var queries = table.ForeignKeys
                    .Select(fc => TranslateAddForeignKey(fc, table))
                    .ToList();
                
                queries.ForEach(x => _sb.Append(x.Sql));
            }

            return CreateQuery();
        }
        
        private Query TranslateSelectAll<T>()
        {
            var table = typeof(T).ToTable();
            
            var columns = table.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{table.Name}\".\"{c.Name}\"");
            
            string columnsString = string.Join(",", columns);
            _sb.Append($"SELECT {columnsString} FROM \"{table.Name}\"");
            return CreateQuery();
        }

        private Query TranslateSelectById<T>(object pk)
        {
            var table = typeof(T).ToTable();
            string selectSql = TranslateSelectAll<T>().Sql;
            var pkParameter = Parameterize(pk);

            _sb
                .Append(selectSql)
                .Append(' ')
                .Append($"WHERE \"{table.PrimaryKey.Name}\" = @{pkParameter.Name}");
            
            return CreateQuery(pkParameter);
        }

        private Query TranslateSaveEntity<T>(T entity)
        {
            var type = entity.GetType();
            var table = type.ToTable();
            
            var parameters = new List<QueryParameter>();
            var columnsForParameters = new Dictionary<QueryParameter, Column>();
            
            // Get parameters for internal fields 
            foreach (var column in table.Columns)
            {
                if (column.IsMapped && !column.IsForeignKey)
                {
                    var value = column.GetValue(entity);
                    var parameter = Parameterize(value);
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
                
                var fkParameter = Parameterize(primaryKey);
                columnsForParameters[fkParameter] = foreignKey.LocalColumn;
                parameters.Add(fkParameter);
            }

            // Create insert/update statement
            var columns = columnsForParameters.Select(p => $"\"{p.Value.Name}\"");
            var values = parameters.Select(p => $"@{p.Name}");
            string columnsString = string.Join(',', columns);
            string valuesString = string.Join(',', values);

            _sb
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
                
                _sb.Append($"\"{column.Name}\" = @{parameter.Name}");

                if (i < parameters.Count - 1)
                {
                    _sb.Append(',');
                }
                
                _sb.Append(Environment.NewLine);
            }

            _sb
                .Append($"RETURNING \"{table.PrimaryKey.Name}\"")
                .Append(Environment.NewLine);

            return CreateQuery(parameters);
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

        private Query TranslateLoadManyToOne<TMany, TOne>(TMany entity)
        {
            var manyTable = typeof(TMany).ToTable();
            var oneTable = typeof(TOne).ToTable();

            // Get primary key property
            var pkProperty = manyTable.GetPropertyForColumn(manyTable.PrimaryKey);
            object? pk = pkProperty?.GetValue(entity);

            if (pk is null)
            {
                throw new OrmException($"Failed to load many to one relationship: no primary key " +
                                       $"found for type {manyTable.Type.Name}");
            }
            
            // Parameterize primary key
            var pkParameter = Parameterize(pk);

            // Build SQL
            var columns = oneTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"t.\"{c.Name}\"");

            string columnString = string.Join(',', columns);
            
            var pkColumn = manyTable.Columns.First(c => c.IsPrimaryKey);
            var fkPkColumn = oneTable.Columns.First(c => c.IsPrimaryKey);
            var fk  = manyTable.ForeignKeys.First(c => c.RemoteTable.Name == oneTable.Name);
            var fkColumn = fk.LocalColumn;

            _sb.Append($"SELECT {columnString} FROM \"{manyTable.Name}\" " +
                       $"JOIN \"{oneTable.Name}\" t on t.\"{fkPkColumn.Name}\" = \"{manyTable.Name}\".\"{fkColumn.Name}\" " +
                       $"WHERE \"{manyTable.Name}\".\"{pkColumn.Name}\" = @{pkParameter.Name}");

            return CreateQuery(pkParameter);
        }

        private Query TranslateLoadOneToMany<TOne, TMany>(TOne entity)
        {
            var oneTable = typeof(TOne).ToTable();
            var manyTable = typeof(TMany).ToTable();

            // Get primary key property
            var pkProperty = oneTable.GetPropertyForColumn(oneTable.PrimaryKey);
            object? pk = pkProperty?.GetValue(entity);
            
            if (pk is null)
            {
                throw new OrmException($"Failed to load many to one relationship: no primary key " +
                                       $"found for type {manyTable.Type.Name}");
            }
            
            // Parameterize primary key
            var pkParameter = Parameterize(pk);

            // Build SQL
            var columns = manyTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(',', columns);
            var fk  = manyTable.ForeignKeys.First(c => c.RemoteTable.Name == oneTable.Name);

            _sb
                .Append($"SELECT {columnsString} FROM \"{manyTable.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{oneTable.Name}\" t")
                .Append(' ')
                .Append("ON")
                .Append(' ')
                .Append($"t.\"{oneTable.PrimaryKey.Name}\" = \"{manyTable.Name}\".\"{fk.LocalColumn.Name}\"")
                .Append(' ')
                .Append($"WHERE t.\"{oneTable.PrimaryKey.Name}\" = @{pkParameter.Name}");

            return CreateQuery(pkParameter);
        }

        private Query TranslateLoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var manyATable = typeof(TManyA).ToTable();
            var manyBTable = typeof(TManyB).ToTable();

            // Find foreign key table for the two types
            var fkTable = manyATable.ForeignKeyTables.First(_ => 
                _.TableA.Type == typeof(TManyA) && _.TableB.Type == typeof(TManyB) || 
                _.TableA.Type == typeof(TManyB) && _.TableB.Type == typeof(TManyA));

            // Find tables for entities
            var fkTableA = fkTable.ForeignKeys.First(fk => 
                fk.RemoteTable is EntityTable t && t.Type == typeof(TManyA));
            
            var fkTableB = fkTable.ForeignKeys.First(fk => 
                fk.RemoteTable is EntityTable t && t.Type == typeof(TManyB));
            
            // Build SQL
            var columns = manyBTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyBTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(",", columns);

            _sb.Append($"SELECT {columnsString} FROM \"{manyATable.Name}\" " +
                       $"JOIN \"{fkTable.Name}\" t ON \"{manyATable.Name}\".\"{manyATable.PrimaryKey.Name}\" = t.\"{fkTableA.LocalColumn.Name}\" " +
                       $"JOIN \"{manyBTable.Name}\" ON t.\"{fkTableB.LocalColumn.Name}\" = \"{manyBTable.Name}\".\"{manyBTable.PrimaryKey.Name}\"");

            return CreateQuery();
        }

        private Query TranslateDropTable(Table table)
        {
            _sb
                .Append($"DROP TABLE IF EXISTS \"{table.Name}\" CASCADE;")
                .Append(Environment.NewLine);

            return CreateQuery();
        }
        
        private Query TranslateCreateTable(Table table)
        {
            _sb
                .Append($"CREATE TABLE \"{table.Name}\"")
                .Append(' ')
                .Append('(')
                .Append(Environment.NewLine);

            int i = 0;
            
            foreach (var column in table.Columns)
            {
                _sb.Append('\t');
                TranslateColumn(column);
                
                if (i < table.Columns.Count - 1)
                {
                    _sb.Append(',');
                }

                _sb.Append(Environment.NewLine);
                i++;
            }

            _sb
                .Append(')')
                .Append(';')
                .Append(Environment.NewLine);

            return CreateQuery();
        }
        
        private void TranslateColumn(Column column)
        {
            var dbType = _typeMapper.Map(column.Type);
            
            if (column.MaxLength.HasValue && dbType is IDbMaxLengthDbType maxLengthType)
            {
                maxLengthType.Length = column.MaxLength.Value;
                dbType = maxLengthType;
            }

            _sb
                .Append($"\"{column.Name}\"")
                .Append(' ')
                .Append(dbType);

            if (column.IsPrimaryKey)
            {
                _sb
                    .Append(' ')
                    .Append("PRIMARY KEY");
            }
                
            if (!column.IsNullable)
            {
                _sb
                    .Append(' ')
                    .Append("NOT NULL");
            }

            if (column.IsUnique)
            {
                _sb
                    .Append(' ')
                    .Append("UNIQUE");
            }
        }
        
        private Query TranslateAddForeignKey(ForeignKey foreignKey, Table table)
        {
            _sb
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

            return CreateQuery();
        }

        private static QueryParameter Parameterize(string name, object? value)
        {
            return new QueryParameter(name, value);
        }
        
        private QueryParameter Parameterize(object? value)
        {
            string parameterName = $"p{++_parameterCount}";
            return new QueryParameter(parameterName, value);
        }
    }
}