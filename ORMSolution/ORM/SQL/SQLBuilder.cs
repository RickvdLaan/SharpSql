using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ORM
{
    internal class SQLBuilder
    {
        private const string Param = "@PARAM";
        internal const string MANY_TO_MANY_JOIN = "MM_Join_";
        internal const string MANY_TO_MANY_JOIN_COUPLER = MANY_TO_MANY_JOIN + "Conn.";
        internal const string MANY_TO_MANY_JOIN_DATA = MANY_TO_MANY_JOIN + "Data.";

        public string GeneratedQuery { get; private set; }

        public readonly Dictionary<string, string> _queryTableNames = new Dictionary<string, string>(5);

        private readonly Dictionary<char, int> _tableCharCounts = new Dictionary<char, int>(5);

        private readonly List<object> _sqlParameters = new List<object>(10);

        public List<(string name, Type type)> TableOrder { get; private set; } = new List<(string name, Type type)>(10);

        public Dictionary<string, int> TableNameColumnCount { get; private set; } = new Dictionary<string, int>();

        public Dictionary<string, string> TableNameResolvePaths { get; private set; } = new Dictionary<string, string>();

        public bool ContainsToManyJoins { get; private set; } = true;

        internal ORMTableAttribute TableAttribute { get; set; }

        internal SqlParameter[] SqlParameters { get; private set; }

        private List<SQLJoin> Joins { get; set; } = new List<SQLJoin>();

        public override string ToString()
        {
            return GeneratedQuery;
        }

        public void BuildQuery(ORMTableAttribute tableAttribute, Expression selectExpression, Expression joinExpression, Expression whereExpression, Expression sortExpression, long maxNumberOfItemsToReturn)
        {
            TableAttribute = tableAttribute;

            AddQueryTableName(TableAttribute);

            var stringBuilder = new StringBuilder();

            // Select is prepended at the end to calculate table counts.

            stringBuilder.Append(From());

            if (joinExpression != null)
            {
                stringBuilder.Append(Join(joinExpression));
            }
            if (whereExpression != null)
            {
                stringBuilder.Append(Where(whereExpression));
            }
            if (sortExpression != null)
            {
                stringBuilder.Append(OrderBy(sortExpression));
            }

            stringBuilder.Insert(0, Select(selectExpression, maxNumberOfItemsToReturn));

            stringBuilder.Append(Semicolon());

            GeneratedQuery = stringBuilder.ToString().ToUpperInvariant();
        }

        public void BuildNonQuery(ORMEntity entity, NonQueryType nonQueryType)
        {
            GeneratedQuery = nonQueryType switch
            {
                NonQueryType.Insert => InsertInto(entity),
                NonQueryType.Update => Update(entity),
                NonQueryType.Delete => Delete(entity),
                _ => throw new NotImplementedException(nonQueryType.ToString()),
            };
        }

        private string InsertInto(ORMEntity entity)
        {
            var stringBuilder = new StringBuilder();

            var tableName = ORMUtilities.CollectionEntityRelations[entity.GetType()].Name;

            stringBuilder.Append($"INSERT INTO [dbo].[{tableName}] (".ToUpperInvariant());

            for (int i = 0; i < entity.TableScheme.Count; i++)
            {
                if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i]))
                    continue;

                var addon = ((entity.TableScheme.Count - entity.PrimaryKey.Count == i) ? string.Empty : ", ");
                stringBuilder.Append($"[dbo].[{tableName}].[{entity.TableScheme[i]}]{addon}".ToUpperInvariant());
            }

            stringBuilder.Append(") VALUES(");

            for (int i = 0; i < entity.TableScheme.Count; i++)
            {
                if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i])) // @ToDo: && !IsAutoIncrement
                    continue;

                var fieldPropertyInfo = entity.GetType().GetProperty(entity.TableScheme[i], entity.PublicFlags);
                var addon = ((entity.TableScheme.Count - entity.PrimaryKey.Count == i) ? string.Empty : ", ");

                if (fieldPropertyInfo.GetValue(entity) is ORMEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    for (int j = 0; j < entityColumnJoin.TableScheme.Count; j++)
                    {
                        // @ToDo: if IsAutoIncrement? continue;
                        var columnName = entityColumnJoin.TableScheme[j];

                        if (entityColumnJoin.PrimaryKey.Keys.Any(x => x.ColumnName == columnName))
                        {
                            stringBuilder.Append(AddSqlParameter((entityColumnJoin.GetType().GetProperty(columnName).GetValue(entityColumnJoin), columnName)));
                            break;
                        }
                    }
                }
                else
                {
                    stringBuilder.Append(AddSqlParameter(entity.SqlValue(entity.TableScheme[i])));
                }

                stringBuilder.Append(addon);
            }

            stringBuilder.Append(")");
            stringBuilder.Append(Semicolon());
            stringBuilder.Append(" SELECT CAST(SCOPE_IDENTITY() AS INT);");

            return stringBuilder.ToString();
        }

        private string Select(long top = -1)
        {
            return top >= 0 ? $"SELECT TOP ({top}) * " : "SELECT * ";
        }

        private string Select(Expression selectExpression, long top = -1)
        {
            if (selectExpression == null)
            {
                foreach (var (name, type) in TableOrder)
                {
                    TableNameColumnCount[name] = ORMUtilities.CachedColumns.ContainsKey(type) ?
                        ORMUtilities.CachedColumns[type].Count : 0;
                }
                return Select(top);
            }

            var parsedExpression = ParseExpression(selectExpression);
            foreach (var (name, _) in TableOrder)
            {
                var matches = Regex.Matches(parsedExpression, $"\\[{name}\\]", RegexOptions.IgnoreCase);
                TableNameColumnCount[name] = matches.Count;
            }

            return top >= 0 ? $"SELECT TOP ({top}) {parsedExpression} " : $"SELECT {parsedExpression} ";
        }

        private string From()
        {
            return $"FROM [dbo].[{TableAttribute.TableName}] AS [{_queryTableNames[TableAttribute.TableName]}]";
        }

        private string From(ORMTableAttribute tableAttribute)
        {
            return $"FROM [dbo].[{tableAttribute.TableName}] AS [{_queryTableNames[tableAttribute.TableName]}]";
        }

        private string Join(Expression expression)
        {
            return ParseExpression(expression);
        }

        private string Where(Expression whereExpression)
        {
            return $" WHERE {ParseExpression(whereExpression)}";
        }

        private string OrderBy(Expression sortExpression)
        {
            return $" ORDER BY {ParseExpression(sortExpression)}";
        }

        private string Update(ORMEntity entity)
        {
            var stringBuilder = new StringBuilder();

            AddQueryTableName(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType()));

            var tableAlias = TableOrder.First(x => x.type == entity.GetType()).name;

            if (entity.IsDirtyList.Any(x => x.IsDirty == true)
            || !entity.IsDirtyList.Any(x => entity.EntityRelations.Any(e => e.GetType().Name != x.ColumnName)))
            {
                stringBuilder.Append($"UPDATE [{tableAlias}] SET ".ToUpperInvariant());
            }

            int entityFieldUpdateCount = 0;
            for (int i = 0; i < entity.TableScheme.Count; i++)
            {
                if ((entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i]) // ToDo: && !entity.AutoIncrement + other locations.
                 || !entity.IsDirtyList[i - 1].IsDirty))
                    continue;

                var fieldPropertyInfo = entity.GetType().GetProperty(entity.TableScheme[i], entity.PublicFlags);

                // Checks if the current entity is a joined entity.
                if (fieldPropertyInfo.GetValue(entity) is ORMEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    // Join object is new, or one or more fields of the join object has dirty fields.
                    if (entityColumnJoin.IsNew && !entityColumnJoin.IsDirtyList.Any(x => x.IsDirty))
                    {
                        AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                    }
                    else if (entity.IsDirtyList[i - 1].IsDirty)
                    {
                        AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                    }
                    else
                    {
                        AddQueryTableName(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[entityColumnJoin.GetType()], entityColumnJoin.GetType()));

                        var tableJoinAlias = TableOrder.First(x => x.type == entityColumnJoin.GetType()).name;

                        for (int j = 0; j < entityColumnJoin.TableScheme.Count; j++)
                        {
                            if (entityColumnJoin.PrimaryKey.Keys.Any(x => x.ColumnName == entityColumnJoin.TableScheme[j]) // ToDo: && !entityColumnJoin.AutoIncrement + other locations.
                            || !entityColumnJoin.IsDirtyList[j - 1].IsDirty)
                                continue;

                            if (entityColumnJoin.IsDirty)
                            {
                                var addon = ((entityColumnJoin.IsDirtyList.Where(x => x.IsDirty == true).Count() <= j) ? string.Empty : ", ");

                                stringBuilder.Append($"[{tableJoinAlias}].[{entityColumnJoin.TableScheme[j]}] = ".ToUpperInvariant() + AddSqlParameter(entityColumnJoin.SqlValue(entityColumnJoin.TableScheme[j])) + (string.IsNullOrEmpty(addon) ? " " : string.Empty));
                            }
                        }
                    }
                }
                else
                {
                    AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                }
            }

            // Where
            if (!entity.IsDirtyList.Any(x => entity.EntityRelations.Any(e => e.GetType().Name != x.ColumnName))
              || entity.IsDirtyList.Any(x => x.IsDirty == true))
            {
                stringBuilder.Append(From(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType())));

                var propertyInfo = entity.GetPrimaryKeyPropertyInfo();

                for (int i = 0; i < propertyInfo.Length; i++)
                {
                    var memberExpression = Expression.Property(Expression.Parameter(entity.GetType(), $"x"), propertyInfo[i]);
                    var constantExpression = Expression.Constant(propertyInfo[i].GetValue(entity), propertyInfo[i].GetValue(entity).GetType());

                    stringBuilder.Append(Where(Expression.Equal(memberExpression, constantExpression)));
                }

                stringBuilder.Append(Semicolon());
            }

            return stringBuilder.ToString();
        }

        private void AddUpdatedParameter(StringBuilder stringBuilder, ORMEntity entity, PropertyInfo propertyInfo, string tableAlias, ref int entityFieldUpdateCount, int currentTableSchemeIndex)
        {
            if (propertyInfo.GetValue(entity) is ORMEntity entityColumnJoin && propertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
            {
                if (entity.PrimaryKey.Keys.Count == 1)
                {
                    var addon = ((entity.IsDirtyList.Where(x => x.IsDirty == true).Count() <= ++entityFieldUpdateCount) ? string.Empty : ", ");

                    stringBuilder.Append($"[{tableAlias}].[{entity.TableScheme[currentTableSchemeIndex]}] = ".ToUpperInvariant() + AddSqlParameter((entityColumnJoin.PrimaryKey.Keys[0].Value, entityColumnJoin.PrimaryKey.Keys[0].ColumnName)) + (string.IsNullOrEmpty(addon) ? " " : addon));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                var addon = ((entity.IsDirtyList.Where(x => x.IsDirty == true).Count() <= ++entityFieldUpdateCount) ? string.Empty : ", ");

                stringBuilder.Append($"[{tableAlias}].[{entity.TableScheme[currentTableSchemeIndex]}] = ".ToUpperInvariant() + AddSqlParameter(entity.SqlValue(entity.TableScheme[currentTableSchemeIndex])) + (string.IsNullOrEmpty(addon) ? " " : addon));
            }
        }

        private string Delete(ORMEntity entity)
        {
            throw new NotImplementedException();
        }

        public string Count(ORMTableAttribute tableAttribute)
        {
            return $"SELECT COUNT(*) FROM {tableAttribute.TableName} AS INT;".ToUpperInvariant();
        }

        private char Semicolon()
        {
            return ';';
        }

        private string ParseExpression(Expression body, string source = null)
        {
            switch (body)
            {
                case BinaryExpression binaryExpression:
                    var left = binaryExpression.Left;
                    var right = binaryExpression.Right;

                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.Equal:
                            return $"({ParseExpression(left)} = {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.LessThan:
                            return $"({ParseExpression(left)} < {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.GreaterThan:
                            return $"({ParseExpression(left)} > {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.LessThanOrEqual:
                            return $"({ParseExpression(left)} <= {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.GreaterThanOrEqual:
                            return $"({ParseExpression(left)} >= {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.Or:
                        case ExpressionType.OrElse:
                            return $"({ParseExpression(left)} OR {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                            return $"({ParseExpression(left)} AND {ParseExpression(right, (left as MemberExpression).Member.Name)})";
                        default:
                            throw new NotImplementedException(body.NodeType.ToString());
                    }
                case MemberExpression memberExpression:
                    {
                        var entityType = memberExpression.Member.ReflectedType;
                        var collectionType = ORMUtilities.CollectionEntityRelations[entityType];

                        return $"[{_queryTableNames[collectionType.Name]}].[{memberExpression.Member.Name}]";
                    }
                case ConstantExpression constantExpression:
                    {
                        return AddSqlParameter((constantExpression.Value, source));
                    }
                case MethodCallExpression methodCallExpression:
                    {
                        if (methodCallExpression.Arguments.OfType<ConstantExpression>().FirstOrDefault() != null)
                        {
                            ParseExpression(methodCallExpression.Arguments.OfType<ConstantExpression>().First());
                        }
                        return methodCallExpression.Method.Name switch
                        {
                            // ORMEntityExtensions.Contains
                            nameof(string.Contains) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Param + _sqlParameters.Count} + '%')",
                            // ORMEntityExtensions.StartsWith
                            nameof(string.StartsWith) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE {Param + _sqlParameters.Count} + '%')",
                            // ORMEntityExtensions.EndsWith
                            nameof(string.EndsWith) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Param + _sqlParameters.Count})",
                            nameof(string.ToString) => ParseExpression(methodCallExpression.Object),
                            nameof(ORMEntityExtensions.Ascending) => $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} ASC",
                            nameof(ORMEntityExtensions.Descending) => $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} DESC",
                            nameof(ORMEntity.Left) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "LEFT"),
                            nameof(ORMEntity.Right) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "RIGHT"),
                            nameof(ORMEntity.Inner) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "INNER"),
                            nameof(ORMEntity.Full) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "FULL"),
                            _ => throw new NotImplementedException(methodCallExpression.Method.Name),
                        };
                    }
                case LambdaExpression lambdaExpression:
                    {
                        return ParseExpression(lambdaExpression.Body);
                    }
                case UnaryExpression unaryExpression:
                    {
                        return ParseExpression(unaryExpression.Operand);
                    }
                case NewArrayExpression newArrayExpression:
                    {
                        var query = string.Empty;

                        for (int i = 0; i < newArrayExpression.Expressions.Count; i++)
                        {
                            if (IsArrayExpressionOfTypeJoin(newArrayExpression))
                            {
                                query += $"{ParseExpression(newArrayExpression.Expressions[i])}";
                            }
                            else
                            {
                                var field = $"{ParseExpression(newArrayExpression.Expressions[i])}";
                                var addon = ((newArrayExpression.Expressions.Count - 1 == i) ? string.Empty : ", ");
                                query += $"{field}{addon}";
                            }
                        }

                        return query;
                    }
                default:
                    throw new NotImplementedException(body.NodeType.ToString());
                case null:
                    throw new ArgumentNullException(nameof(body));
            }
        }

        private string AddSqlParameter((object value, string sourceColumn) mappedValue)
        {
            _sqlParameters.Add(mappedValue.value);

            if (SqlParameters == null)
            {
                SqlParameters = new SqlParameter[_sqlParameters.Count];
            }
            else
            {
                // @FixMe, @Performance, @Quick&Dirty: This makes me sad... but we also don't want SqlParameters to become
                // a variable. Fix at some point in the future.
                //
                // Note that this is being done with EVERY parameter that gets added :) so allow me to
                // cry here a bit... or a lot... :)
                SqlParameter[] copySqlParameters = new SqlParameter[SqlParameters.Length];
                Array.Copy(SqlParameters, copySqlParameters, SqlParameters.Length);
                Array.Resize(ref copySqlParameters, _sqlParameters.Count);
                SqlParameters = copySqlParameters;
            }

            SqlParameters[_sqlParameters.Count - 1] = new SqlParameter(Param + _sqlParameters.Count, _sqlParameters[^1])
            {
                SourceColumn = mappedValue.sourceColumn
            };

            return $"{Param + _sqlParameters.Count}";
        }

        private void AddQueryTableName(ORMTableAttribute table)
        {
            var tableName = table.TableName;
            char firstChar = tableName[0];

            if (_tableCharCounts.ContainsKey(firstChar))
            {
                _tableCharCounts[firstChar] = ++_tableCharCounts[firstChar];
            }
            else
            {
                _tableCharCounts[firstChar] = 1;
            }

            var usedName = new string(firstChar, _tableCharCounts[firstChar]);

            TableOrder.Add((usedName, table.EntityType));
            _queryTableNames[tableName] = usedName;
        }

        private SQLJoin CalculateJoins(ORMTableAttribute tableAttribute, string tableName)
        {
            var propertyInfo = tableAttribute.EntityType.GetProperty(tableName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            var rightTableAttribute = ORMUtilities.CollectionEntityRelations[propertyInfo.PropertyType].GetCustomAttributes(typeof(ORMTableAttribute), true).First() as ORMTableAttribute;
            AddQueryTableName(rightTableAttribute);

            var rightInstance = (ORMEntity)Activator.CreateInstance(propertyInfo.PropertyType);

            SQLJoin join = new SQLJoin()
            {
                LeftTableAttribute = tableAttribute,
                LeftPropertyInfo = propertyInfo,
                RightTableAttribute = rightTableAttribute,
                RightPropertyInfo = rightInstance.GetPrimaryKeyPropertyInfo()
            };

            // Lookup parent path if available and add this current path to the list
            var parentTableName = _queryTableNames[tableAttribute.TableName];
            var basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : "";
            TableNameResolvePaths.Add(_queryTableNames[rightTableAttribute.TableName], basePath + propertyInfo.Name());

            return join;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateJoinQuery(MemberExpression expression, string joinType)
        {
            var targetProperty = expression.Member.DeclaringType.GetProperty(expression.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            var relations = ORMUtilities.ManyToManyRelations.GetValueOrDefault((ORMUtilities.CollectionEntityRelations[expression.Member.DeclaringType], targetProperty.PropertyType));
            var stringBuilder = new StringBuilder();

            if (relations != null)
            {
                ContainsToManyJoins = true;

                var properties = relations.EntityType.GetProperties();

                SQLJoin firstJoin = new SQLJoin()
                {
                    LeftTableAttribute = TableAttribute,
                    LeftPropertyInfo = TableAttribute.EntityType.GetProperties().Where(x => (x.GetCustomAttributes(typeof(ORMPrimaryKeyAttribute), true).FirstOrDefault() as ORMPrimaryKeyAttribute) != null).First(),
                    RightTableAttribute = relations.CollectionType.GetCustomAttribute<ORMTableAttribute>(),
                    RightPropertyInfo = properties.Where(x => (x.GetCustomAttributes(typeof(ORMForeignKeyAttribute), true).FirstOrDefault() as ORMForeignKeyAttribute)?.Relation == expression.Member.DeclaringType).ToArray()
                };

                SQLJoin secondJoin = new SQLJoin()
                {
                    LeftTableAttribute = relations.CollectionType.GetCustomAttribute<ORMTableAttribute>(),
                    LeftPropertyInfo = properties.Where(x => (x.GetCustomAttributes(typeof(ORMForeignKeyAttribute), true).FirstOrDefault() as ORMForeignKeyAttribute)?.Relation == ORMUtilities.CollectionEntityRelations[targetProperty.PropertyType]).First(),
                    RightTableAttribute = targetProperty.PropertyType.GetCustomAttribute<ORMTableAttribute>(),
                    RightPropertyInfo = ORMUtilities.CollectionEntityRelations[targetProperty.PropertyType].GetProperties().Where(x => (x.GetCustomAttributes(typeof(ORMPrimaryKeyAttribute), true).FirstOrDefault() as ORMPrimaryKeyAttribute) != null).ToArray()
                };

                AddQueryTableName(firstJoin.RightTableAttribute);
                AddQueryTableName(secondJoin.RightTableAttribute);

                Joins.Add(firstJoin);
                Joins.Add(secondJoin);

                GenerateJoinSql(joinType, stringBuilder, firstJoin);
                GenerateJoinSql(joinType, stringBuilder, secondJoin);

                var parentTableName = _queryTableNames[firstJoin.RightTableAttribute.TableName];
                var basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : MANY_TO_MANY_JOIN_COUPLER;
                TableNameResolvePaths.Add(_queryTableNames[firstJoin.RightTableAttribute.TableName], basePath + targetProperty.Name());

                parentTableName = _queryTableNames[secondJoin.RightTableAttribute.TableName];
                basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : MANY_TO_MANY_JOIN_DATA;
                TableNameResolvePaths.Add(_queryTableNames[secondJoin.RightTableAttribute.TableName], basePath + targetProperty.Name());

                return stringBuilder.ToString();
            }

            var join = CalculateJoins(TableAttribute, expression.Member.Name);
            Joins.Add(join);
            GenerateJoinSql(joinType, stringBuilder, join);
            return stringBuilder.ToString();
        }

        private void GenerateJoinSql(string joinType, StringBuilder stringBuilder, SQLJoin join)
        {
            for (int i = 0; i < join.RightPropertyInfo.Length; i++)
            {
                stringBuilder.Append($" {joinType} JOIN [dbo].[{join.RightTableAttribute.TableName}] AS [{_queryTableNames[join.RightTableAttribute.TableName]}] ON [{_queryTableNames[join.LeftTableAttribute.TableName]}].[{join.LeftPropertyInfo.Name()}] = [{_queryTableNames[join.RightTableAttribute.TableName]}].[{join.RightPropertyInfo[i].Name()}]");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsArrayExpressionOfTypeJoin(NewArrayExpression newArrayExpression)
        {
            return newArrayExpression.Expressions
                                     .OfType<MethodCallExpression>()
                                     .Any(x => x.Method.Name == nameof(ORMEntity.Left)
                                            || x.Method.Name == nameof(ORMEntity.Right)
                                            || x.Method.Name == nameof(ORMEntity.Inner)
                                            || x.Method.Name == nameof(ORMEntity.Full));
        }
    }
}