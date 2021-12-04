using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Npgsql;
using ORM.Core.Interfaces;
using ORM.Core.Models;
using ORM.Core.Models.Enums;
using ORM.Core.Models.Extensions;
using ORM.Postgres.Interfaces;

namespace ORM.Postgres.SqlDialect
{
    public class PostgresCommandBuilder : ICommandBuilder
    {
        private readonly NpgsqlConnection _connection;

        private readonly IDbTypeMapper _typeMapper;
        
        private StringBuilder _sb = new StringBuilder();

        private int _parameterCount = 0;
        
        private int _tableCount = 0;

        private Type? _lastType = null;
        
        public PostgresCommandBuilder(NpgsqlConnection connection, IDbTypeMapper typeMapper)
        {
            _connection = connection;
            _typeMapper = typeMapper;
        }

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

        public IDbCommand BuildGetAll<T>()
        {
            var query = TranslateSelect<T>();
            return CreateCommand(query);
        }
        
        public IDbCommand BuildGetById<T>(object pk)
        {
            var query = TranslateSelectById<T>(pk);
            return CreateCommand(query);
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

        private IDbCommand CreateCommand(Query query, bool newConnection = false)
        {
            var connection = newConnection ? CreateConnection() : _connection;
            var cmd = connection.CreateCommand();
            cmd.CommandText = query.Sql;
            
            foreach (var value in query.Parameters)
            {
                if (value.IsParameterized)
                {
                    cmd.Parameters.AddWithValue(value.Name, value.Value);
                }
            }

            _tableCount = 0;
            _parameterCount = 0;
            
            Console.WriteLine(cmd.CommandText);
            
            return cmd;
        }

        public Query TranslateCreateTables(List<Table> tables)
        {
            var queries = tables.Select(TranslateCreateTable).ToList();
            queries.ForEach(x => _sb.Append(x.Sql));
            return CreateQuery();
        }

        public Query TranslateDropTables(List<Table> tables)
        {
            var queries = tables.Select(TranslateDropTable).ToList();
            queries.ForEach(x => _sb.Append(x.Sql));
            return CreateQuery();
        }
        
        public Query TranslateAddForeignKeys(List<Table> tables)
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
        
        private Query TranslateSelect<T>()
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
            // get primary key
            var table = typeof(T).ToTable();
            var properties = table.Type.GetProperties();
            var pkProperty = properties.First(p => new Column(p).Name == table.PrimaryKey.Name);

            var parameter = GetParameter(pkProperty, pk);
            string selectSql = TranslateSelect<T>().Sql;
            
            _sb
                .Append(selectSql)
                .Append(' ')
                .Append($"WHERE \"{table.PrimaryKey.Name}\" = @{parameter.Name}");
            
            return CreateQuery(parameter);
        }

        private Query TranslateSave<T>(T entity)
        {
            var saveCollectionQuery = TranslateSaveCollection(entity);

            if (saveCollectionQuery.Sql != string.Empty)
            {
                return saveCollectionQuery;
            }
            
            var table = entity.GetType().ToTable();
            var parameters = GetParameters(entity);
            var saveEntityParameters = parameters.ToList();
            
            var saveForeignKeyEntities =  TranslateSaveOneToManyEntities(entity, saveEntityParameters);
            saveForeignKeyEntities.Parameters.ForEach(p => parameters.Add(p));
            _sb.Append(saveForeignKeyEntities.Sql);
            
            var entityColumns = saveEntityParameters.Select(p => p.Column);
            var entityValues = saveEntityParameters.Select(p => p.IsParameterized ? $"@{p.Name}" : p.Value);
            string entityColumnsString = string.Join(',', entityColumns);
            string entityValuesString = string.Join(',', entityValues);

            _sb
                .Append($"INSERT INTO \"{table.Name}\" ({entityColumnsString})")
                .Append(' ')
                .Append(Environment.NewLine)
                .Append($"VALUES ({entityValuesString})")
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

            for (int i = 0; i < saveEntityParameters.Count; i++)
            {
                var parameter = saveEntityParameters[i];
                object columnValue = parameter.IsParameterized ? $"@{parameter.Name}" : parameter.Value;
                _sb.Append($"{parameter.Column} = {columnValue}");

                if (i < saveEntityParameters.Count - 1)
                {
                    _sb.Append(',');
                }
                
                _sb.Append(Environment.NewLine);
            }
            
            _sb.Append($"RETURNING \"{table.PrimaryKey.Name}\"");
            return CreateQuery(parameters);
        }


        private Query TranslateSaveCollection<T>(T entity)
        {
            var table = entity.GetType().ToTable();
            var parameters = new List<QueryParameter>();
            var saveCollectionSqlBuilder = new StringBuilder();

            // get properties on entity which store a collection
            var collectionProperties = table.GetPropertiesOf(RelationshipType.OneToMany);
            
            foreach (var property in collectionProperties)
            {
                // to avoid recursion
                if (_lastType == table.Type)
                {
                    break;
                }
                
                _lastType = table.Type;

                // get the collection
                var collection = property.GetValue(entity) as IEnumerable ?? new List<object?>();
                
                // get the navigated property of the collection item type
                var collectionItemType = property.PropertyType.GetUnderlyingType();
                var navigatedProperty = collectionItemType.GetNavigatedProperty(table.Type);
                if (navigatedProperty is null) continue;
                
                // create query to save items of collection
                foreach (var item in collection)
                {
                    // set entity on collection item
                    navigatedProperty.SetValue(item, entity);
                    
                    // create query
                    var itemQuery = TranslateSave(item);
                    itemQuery.Parameters.ForEach(parameters.Add);
                    
                    saveCollectionSqlBuilder
                        .Append(itemQuery.Sql)
                        .Append(';')
                        .Append(Environment.NewLine);
                }
            }

            string sql = saveCollectionSqlBuilder.ToString();
            _sb.Append(sql);
            return CreateQuery(parameters);
        }

        private Query TranslateSaveOneToManyEntities<T>(T entity, List<QueryParameter> entityParameter)
        {
            var parameters = new List<QueryParameter>();
            var fkParameters = GetForeignKeyParameters(entity);

            for (int i = 0; i < fkParameters.Count; i++)
            {
                var parameter = fkParameters[i];
                var fieldTable = parameter.Value.GetType().ToTable();
                var saveFieldQuery = TranslateSave(parameter.Value);
                saveFieldQuery.Parameters.ForEach(p => parameters.Add(p));

                _sb
                    .Append($"WITH table{_tableCount} AS")
                    .Append(' ')
                    .Append('(')
                    .Append(Environment.NewLine)
                    .Append(saveFieldQuery.Sql)
                    .Append(Environment.NewLine)
                    .Append(')');
                
                string selectFkSql = $"(SELECT \"{fieldTable.PrimaryKey.Name}\" FROM table{_tableCount})";
                var fkParameter = new QueryParameter($"\"{parameter.Column}\"", selectFkSql);
                
                parameters.Add(fkParameter);
                entityParameter.Add(fkParameter);

                if (i < fkParameters.Count - 1)
                {
                    _sb.Append(',');
                }
                
                _tableCount++;
                _sb.Append(Environment.NewLine);
            }

            return CreateQuery(parameters);
        }
        
        public IDbCommand BuildSave<T>(T entity)
        {
            var query = TranslateSave(entity);
            return CreateCommand(query);
        }

        private KeyValuePair<string, object> Parameterize(object value)
        {
            string parameterName = $"p{++_parameterCount}";
            var kv = new KeyValuePair<string, object>(parameterName, value);
            return kv;
        }

        public IDbCommand BuildLoadManyToOne<TMany, TOne>(TMany entity)
        {
            var query = TranslateLoadManyToOne<TMany, TOne>(entity);
            return CreateCommand(query, newConnection: true);
        }

        public IDbCommand BuildLoadOneToMany<TOne, TMany>(TOne entity)
        {
            var query = TranslateLoadOneToMany<TOne, TMany>(entity);
            return CreateCommand(query, newConnection: true);
        }

        public IDbCommand BuildLoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var query = TranslateLoadManyToMany<TManyA, TManyB>(entity);
            return CreateCommand(query, newConnection: true);
        }

        private Query TranslateLoadManyToOne<TMany, TOne>(TMany entity)
        {
            var manyTable = typeof(TMany).ToTable();
            var oneTable = typeof(TOne).ToTable();
            
            var pkProperty = typeof(TMany).GetProperties().First(p => new Column(p).Name == manyTable.PrimaryKey.Name);
            object pk = pkProperty.GetValue(entity);

            var columns = oneTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"t.\"{c.Name}\"");

            string columnString = string.Join(',', columns);
            
            var pkColumn = manyTable.Columns.First(c => c.IsPrimaryKey);
            var fkPkColumn = oneTable.Columns.First(c => c.IsPrimaryKey);
            var fk  = manyTable.ForeignKeys.First(c => c.TableTo.Name == oneTable.Name);
            var fkColumn = fk.ColumnFrom;

            _sb.Append($"SELECT {columnString} FROM \"{manyTable.Name}\" " +
                       $"JOIN \"{oneTable.Name}\" t on t.\"{fkPkColumn.Name}\" = \"{manyTable.Name}\".\"{fkColumn.Name}\" " +
                       $"WHERE \"{manyTable.Name}\".\"{pkColumn.Name}\" = {pk}");

            return CreateQuery();
        }

        private Query TranslateLoadOneToMany<TOne, TMany>(TOne entity)
        {
            var oneTable = typeof(TOne).ToTable();
            var manyTable = typeof(TMany).ToTable();

            // get primary key property
            var pkProperty = typeof(TOne).GetProperties().First(p => new Column(p).Name == oneTable.PrimaryKey.Name);
            object pk = pkProperty.GetValue(entity);
            
            // parameterize primary key
            (string paramName, object paramValue) = Parameterize(pk);
            var pkParam = new QueryParameter(paramName, paramValue);

            // build sql
            var columns = manyTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(',', columns);
            var fk  = manyTable.ForeignKeys.First(c => c.TableTo.Name == oneTable.Name);

            _sb
                .Append($"SELECT {columnsString} FROM \"{manyTable.Name}\"")
                .Append(' ')
                .Append($"JOIN \"{oneTable.Name}\" t")
                .Append(' ')
                .Append("ON")
                .Append(' ')
                .Append($"t.\"{oneTable.PrimaryKey.Name}\" = \"{manyTable.Name}\".\"{fk.ColumnFrom.Name}\"")
                .Append(' ')
                .Append($"WHERE t.\"{oneTable.PrimaryKey.Name}\" = @{pkParam.Name}");

            return CreateQuery(pkParam);
        }

        private Query TranslateLoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var manyATable = typeof(TManyA).ToTable();
            var manyBTable = typeof(TManyB).ToTable();

            var fkTable = manyATable.ForeignKeyTables.First(_ => 
                _.TableA.Type == typeof(TManyA) && _.TableB.Type == typeof(TManyB) || 
                _.TableA.Type == typeof(TManyB) && _.TableB.Type == typeof(TManyA));

            var fkTableA = fkTable.ForeignKeys.First(fk => 
                fk.TableTo is EntityTable t && t.Type == typeof(TManyA));
            
            var fkTableB = fkTable.ForeignKeys.First(fk => 
                fk.TableTo is EntityTable t && t.Type == typeof(TManyB));
            
            var columns = manyBTable.Columns
                .Where(c => c.IsMapped)
                .Select(c => $"\"{manyBTable.Name}\".\"{c.Name}\"");

            string columnsString = string.Join(",", columns);

            _sb.Append($"SELECT {columnsString} FROM \"{manyATable.Name}\" " +
                       $"JOIN \"{fkTable.Name}\" t ON \"{manyATable.Name}\".\"{manyATable.PrimaryKey.Name}\" = t.\"{fkTableA.ColumnFrom.Name}\" " +
                       $"JOIN \"{manyBTable.Name}\" ON t.\"{fkTableB.ColumnFrom.Name}\" = \"{manyBTable.Name}\".\"{manyBTable.PrimaryKey.Name}\"");

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
                .Append($"\"{foreignKey.ColumnFrom.Name}_{foreignKey.ColumnTo.Name}\"")
                .Append(' ')
                .Append("FOREIGN KEY")
                .Append('(')
                .Append($"\"{foreignKey.ColumnFrom.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("REFERENCES")
                .Append(' ')
                .Append($"\"{foreignKey.TableTo.Name}\"")
                .Append('(')
                .Append($"\"{foreignKey.ColumnTo.Name}\"")
                .Append(')')
                .Append(' ')
                .Append("ON DELETE CASCADE")
                .Append(';')
                .Append(Environment.NewLine);

            return CreateQuery();
        }

        private List<QueryParameter> GetParameters<T>(T entity)
        {
            var type = entity.GetType();
            var table = type.ToTable();
            var properties = type.GetProperties();
            
            return table.Columns
                .Where(c => c.IsMapped && !c.IsForeignKey)
                .Select(c => 
                {
                    var property = properties.First(p => new Column(p).Name == c.Name);
                    object value = property.GetValue(entity) ?? DBNull.Value;
                    return GetParameter(property, value);
                })
                .ToList();
        }

        private List<QueryParameter> GetForeignKeyParameters<T>(T entity)
        {
            var type = entity.GetType();
            var table = type.ToTable();
            var properties = type.GetProperties();
            var parameters = new List<QueryParameter>();
            
            foreach (var fk in table.ForeignKeys)
            {
                if (fk.TableTo is not EntityTable entityTable) continue;
                var property = properties.First(p => p.PropertyType == entityTable.Type);
                
                object? value = property.GetValue(entity);
                if (value is null) continue;
                
                var (paramName, paramValue) = Parameterize(value);
                var parameter = new QueryParameter(fk.ColumnFrom.Name, paramName, paramValue);
                parameters.Add(parameter);
            }

            return parameters;
        }

        private QueryParameter GetParameter(PropertyInfo property, object value)
        {
            var column = new Column(property);
            var (paramName, paramValue) = Parameterize(value);
            return new QueryParameter($"\"{column.Name}\"", paramName, paramValue);
        }
    }
}