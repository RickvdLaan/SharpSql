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
            switch (nonQueryType)
            {
                case NonQueryType.Insert:
                    GeneratedQuery = InsertInto(entity);
                    break;
                case NonQueryType.Update:
                    GeneratedQuery = Update(entity);
                    break;
                case NonQueryType.Delete:
                    GeneratedQuery = Delete(entity);
                    break;
                default:
                    throw new NotImplementedException(nonQueryType.ToString());
            }
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
                if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i]))
                    continue;

                var fieldPropertyInfo = entity.GetType().GetProperty(entity.TableScheme[i], entity.PublicFlags);
                if (fieldPropertyInfo.GetValue(entity) is ORMEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    for (int j = 0; j < entityColumnJoin.TableScheme.Count; j++)
                    {
                        if (entityColumnJoin.PrimaryKey.Keys.Any(x => x.ColumnName == entityColumnJoin.TableScheme[j]))
                        {
                            stringBuilder.Append($"'{entityColumnJoin.GetType().GetProperty(entityColumnJoin.TableScheme[j]).GetValue(entityColumnJoin)}'");
                            break;
                        }
                    }
                }
                else
                {
                    var addon = ((entity.TableScheme.Count - entity.PrimaryKey.Count == i) ? string.Empty : ", ");

                    stringBuilder.Append(entity.SqlValue(entity.TableScheme[i], addon));
                }
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
            return $" WHERE {ParseWhereExpression(whereExpression)}";
        }

        private string OrderBy(Expression sortExpression)
        {
            return $" ORDER BY {ParseExpression(sortExpression)}";
        }

        private string Update(ORMEntity entity, bool ignoreStuff = false)
        {
            var stringBuilder = new StringBuilder();

            AddQueryTableName(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType()));

            var tableAlias = TableOrder.First(x => x.type == entity.GetType()).name;

            if (entity.IsDirtyList.Any(x => x.IsDirty == true)
            || !entity.IsDirtyList.Any(x => entity.EntityRelations.Any(e => e.GetType().Name != x.ColumnName)))
            {
                stringBuilder.Append($"UPDATE [{tableAlias}] SET ".ToUpperInvariant());
            }

            for (int i = 0; i < entity.TableScheme.Count; i++)
            {
                if (!ignoreStuff
                 && (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i])
                 || !entity.IsDirtyList[i - 1].IsDirty))
                    continue;

                var fieldPropertyInfo = entity.GetType().GetProperty(entity.TableScheme[i], entity.PublicFlags);
                if (fieldPropertyInfo.GetValue(entity) is ORMEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(ORMEntity)))
                {
                    if (entityColumnJoin.IsDirty && entityColumnJoin.IsNew && !entityColumnJoin.IsDirtyList.Any(x => x.IsDirty))
                    {
                        stringBuilder.Insert(0, $"{Update(entityColumnJoin, true)} ");

                        if (entity.IsDirtyList.Any(x => x.IsDirty == true))
                        {
                            if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i]))
                                continue;

                            var addon = ((entity.IsDirtyList.Where(x => x.IsDirty == true).Count() < i) ? string.Empty : ", ");

                            stringBuilder.Append($"[{tableAlias}].[{entity.TableScheme[i]}] = ".ToUpperInvariant() + ParseSqlParameters(addon) + " ");
                        }
                    }
                    else
                    {
                        AddQueryTableName(new ORMTableAttribute(ORMUtilities.CollectionEntityRelations[entityColumnJoin.GetType()], entityColumnJoin.GetType()));

                        var tableJoinAlias = TableOrder.First(x => x.type == entityColumnJoin.GetType()).name;

                        for (int j = 0; j < entityColumnJoin.TableScheme.Count; j++)
                        {
                            if (entityColumnJoin.PrimaryKey.Keys.Any(x => x.ColumnName == entityColumnJoin.TableScheme[j])
                            || !entityColumnJoin.IsDirtyList[j - 1].IsDirty)
                                continue;

                            if (entityColumnJoin.IsDirty)
                            {
                                var addon = ((entityColumnJoin.IsDirtyList.Where(x => x.IsDirty == true).Count() <= j) ? string.Empty : ", ");

                                stringBuilder.Append($"[{tableJoinAlias}].[{entityColumnJoin.TableScheme[j]}] = ".ToUpperInvariant() + entityColumnJoin.SqlValue(entityColumnJoin.TableScheme[j], addon) + (string.IsNullOrEmpty(addon) ? " " : string.Empty));
                            }
                        }
                    }
                }
                else
                {
                    if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.TableScheme[i]))
                        continue;

                    var addon = ((entity.IsDirtyList.Where(x => x.IsDirty == true).Count() <= i - 1) ? string.Empty : ", ");

                    stringBuilder.Append($"[{tableAlias}].[{entity.TableScheme[i]}] = ".ToUpperInvariant() + entity.SqlValue(entity.TableScheme[i], addon) + (string.IsNullOrEmpty(addon) ? " " : string.Empty));
                }
            }

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

        private string ParseWhereExpression(Expression whereExpression)
        {
            var where = ParseExpression(whereExpression);

            SqlParameters = new SqlParameter[_sqlParameters.Count];

            for (int i = 0; i < _sqlParameters.Count; i++)
            {
                SqlParameters[i] = new SqlParameter(Param + (i + 1), _sqlParameters[i]);
            }

            return where;
        }

        private string ParseExpression(Expression body, bool useCache = true)
        {
            switch (body)
            {
                case BinaryExpression binaryExpression:
                    var left = binaryExpression.Left;
                    var right = binaryExpression.Right;

                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.Equal:
                            return $"({ParseExpression(left)} = {ParseExpression(right)})";
                        case ExpressionType.LessThan:
                            return $"({ParseExpression(left)} < {ParseExpression(right)})";
                        case ExpressionType.GreaterThan:
                            return $"({ParseExpression(left)} > {ParseExpression(right)})";
                        case ExpressionType.LessThanOrEqual:
                            return $"({ParseExpression(left)} <= {ParseExpression(right)})";
                        case ExpressionType.GreaterThanOrEqual:
                            return $"({ParseExpression(left)} >= {ParseExpression(right)})";
                        case ExpressionType.Or:
                        case ExpressionType.OrElse:
                            return $"({ParseExpression(left)} OR {ParseExpression(right)})";
                        case ExpressionType.And:
                        case ExpressionType.AndAlso:
                            return $"({ParseExpression(left)} AND {ParseExpression(right)})";
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
                        _sqlParameters.Add(constantExpression.Value);

                        return $"{Param + _sqlParameters.Count}";
                    }
                case MethodCallExpression methodCallExpression:
                    {
                        if (methodCallExpression.Arguments.OfType<ConstantExpression>().FirstOrDefault() != null)
                        {
                            ParseExpression(methodCallExpression.Arguments.OfType<ConstantExpression>().First());
                        }
                        switch (methodCallExpression.Method.Name)
                        {
                            case nameof(string.Contains):   // ORMEntityExtensions.Contains
                                return $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.StartsWith): // ORMEntityExtensions.StartsWith
                                return $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.EndsWith):   // ORMEntityExtensions.EndsWith
                                return $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Param + _sqlParameters.Count})";
                            case nameof(string.ToString):
                                return ParseExpression(methodCallExpression.Object);
                            case nameof(ORMEntityExtensions.Ascending):
                                return $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} ASC";
                            case nameof(ORMEntityExtensions.Descending):
                                return $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} DESC";
                            case nameof(ORMEntity.Left):
                                return GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "LEFT");
                            case nameof(ORMEntity.Right):
                                return GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "RIGHT");
                            case nameof(ORMEntity.Inner):
                                return GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "INNER");
                            case nameof(ORMEntity.Full):
                                return GenerateJoinQuery(methodCallExpression.Object as MemberExpression, "FULL");
                            default:
                                throw new NotImplementedException(methodCallExpression.Method.Name);
                        }
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
            TableNameResolvePaths.Add(_queryTableNames[rightTableAttribute.TableName], basePath + propertyInfo.Name);

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
                TableNameResolvePaths.Add(_queryTableNames[firstJoin.RightTableAttribute.TableName], basePath + targetProperty.Name);

                parentTableName = _queryTableNames[secondJoin.RightTableAttribute.TableName];
                basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : MANY_TO_MANY_JOIN_DATA;
                TableNameResolvePaths.Add(_queryTableNames[secondJoin.RightTableAttribute.TableName], basePath + targetProperty.Name);

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
                stringBuilder.Append($" {joinType} JOIN [dbo].[{join.RightTableAttribute.TableName}] AS [{_queryTableNames[join.RightTableAttribute.TableName]}] ON [{_queryTableNames[join.LeftTableAttribute.TableName]}].[{join.LeftPropertyInfo.Name}] = [{_queryTableNames[join.RightTableAttribute.TableName]}].[{join.RightPropertyInfo[i].Name}]");
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

        private string ParseSqlParameters(string addon)
        {
            foreach (var sqlParameter in SqlParameters)
            {
                if (sqlParameter == null)
                {
                    return $"NULL{addon}";
                }
                else
                {
                    return $"{sqlParameter}{addon}";
                }
            }

            return string.Empty;
        }
    }
}