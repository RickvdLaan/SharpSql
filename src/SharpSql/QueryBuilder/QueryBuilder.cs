using Microsoft.Data.SqlClient;
using SharpSql.Attributes;
using SharpSql.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpSql
{
    internal class QueryBuilder
    {
        public string GeneratedQuery { get; private set; }

        public readonly Dictionary<string, string> _queryTableNames = new(5);

        private readonly Dictionary<char, int> _tableCharCounts = new(5);

        internal NonQueryType NonQueryType { get; private set; }

        public List<(string Name, Type Type)> TableOrder { get; private set; } = new List<(string name, Type type)>(10);

        public Dictionary<string, int> TableNameColumnCount { get; private set; } = new Dictionary<string, int>();

        public Dictionary<string, string> TableNameResolvePaths { get; private set; } = new Dictionary<string, string>();

        public bool ContainsToManyJoins { get; private set; } = false;

        internal SharpSqlTableAttribute TableAttribute { get; set; }

        internal List<SqlParameter> SqlParameters { get; private set; } = new List<SqlParameter>(16);

        internal List<RelationalJoin> Joins { get; set; } = new List<RelationalJoin>();

        public override string ToString()
        {
            return GeneratedQuery;
        }

        public void BuildQuery(SharpSqlTableAttribute tableAttribute, Expression selectExpression, Expression joinExpression, Expression whereExpression, Expression sortExpression, long maxNumberOfItemsToReturn)
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

            stringBuilder.Append(Constants.Semicolon);

            GeneratedQuery = stringBuilder.ToString();
        }

        public void BuildNonQuery(SharpSqlEntity entity, NonQueryType nonQueryType)
        {
            GeneratedQuery = (NonQueryType = nonQueryType) switch
            {
                NonQueryType.Insert => InsertInto(entity),
                NonQueryType.Update => Update(entity),
                NonQueryType.Delete => Delete(entity),
                _ => throw new NotImplementedException(nonQueryType.ToString()),
            };
        }

        private string InsertInto(SharpSqlEntity entity)
        {
            var stringBuilder = new StringBuilder();

            var tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);

            stringBuilder.Append($"INSERT INTO [DBO].[{tableName}] (");

            for (int i = 0; i < entity.MutableTableScheme.Count; i++)
            {
                var addon = (i >= entity.MutableTableScheme.Count - 1) ? string.Empty : ", ";
                stringBuilder.Append($"[DBO].[{tableName}].[{entity.MutableTableScheme[i]}]{addon}");
            }

            stringBuilder.Append(") VALUES(");

            for (int i = 0; i < entity.MutableTableScheme.Count; i++)
            {
                var fieldPropertyInfo = entity.GetPropertyInfo(entity.MutableTableScheme[i]);
                var addon = (i >= entity.MutableTableScheme.Count - 1) ? string.Empty : ", ";

                if (fieldPropertyInfo.GetValue(entity) is SharpSqlEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(SharpSqlEntity)))
                {
                    if (entity.DirtyTracker.IsDirty(fieldPropertyInfo.Name)
                     || entity.IsNew) // ObjectState is new, therefore the join has to be added.
                    {
                        stringBuilder.Append(AddSqlParameter((entityColumnJoin.GetType().GetProperty(entityColumnJoin.PrimaryKey.Keys[0].ColumnName).GetValue(entityColumnJoin), fieldPropertyInfo.Name)));
                    }
                }
                else
                {
                    stringBuilder.Append(AddSqlParameter(entity.SqlValue(entity.MutableTableScheme[i])));
                }

                stringBuilder.Append(addon);
            }

            stringBuilder.Append(')');
            stringBuilder.Append(Constants.Semicolon);

            if (!entity.PrimaryKey.IsCombinedPrimaryKey)
            {
                if (entity.IsAutoIncrement)
                {
                    stringBuilder.Append(" SELECT CAST(SCOPE_IDENTITY() AS INT);");
                }
            }

            return stringBuilder.ToString();
        }

        private static string Select(long top = -1)
        {
            return top >= 0 ? $"SELECT TOP ({top}) * " : "SELECT * ";
        }

        private string Select(Expression selectExpression, long top = -1)
        {
            if (selectExpression == null)
            {
                foreach (var (name, type) in TableOrder)
                {
                    TableNameColumnCount[name] = SharpSqlUtilities.CachedColumns.ContainsKey(type) ?
                        SharpSqlUtilities.CachedColumns[type].Count : 0;
                }
                return Select(top);
            }

            var parsedExpression = ParseExpression(selectExpression);
            foreach (var (name, _) in TableOrder)
            {
                var matches = Regex.Matches(parsedExpression, $"\\[{name}\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
                TableNameColumnCount[name] = matches.Count;
            }

            return top >= 0 ? $"SELECT TOP ({top}) {parsedExpression} " : $"SELECT {parsedExpression} ";
        }

        private string From()
        {
            return $"FROM [DBO].[{TableAttribute.TableName}] AS [{_queryTableNames[TableAttribute.TableName]}]";
        }

        private string From(SharpSqlTableAttribute tableAttribute)
        {
            return $"FROM [DBO].[{tableAttribute.TableName}] AS [{_queryTableNames[tableAttribute.TableName]}]";
        }

        private string Join(Expression expression)
        {
            // If no join type has been provided, it'll automatically use a left join.
            // Anonymous types cannot provide a join type, it's automatically a left join.
            return ParseExpression(ParseJoinExpression(expression));
        }

        private Expression ParseJoinExpression(Expression expression)
        {
            if (expression is LambdaExpression lambdaExpression)
            {
                // Anonymous types
                if (lambdaExpression.Body is NewExpression newExpression)
                {
                    if (newExpression.Arguments.Count > 1)
                    {
                        throw new NotImplementedException();
                        //var expressions = new List<MemberExpression>(newExpression.Arguments.Count);

                        //for (int i = 0; i < newExpression.Arguments.Count; i++)
                        //{
                        //    expressions.Add(newExpression.Arguments[i] as MemberExpression);
                        //}

                        //return Expression.NewArrayInit(typeof(object), expressions);
                    }

                    return ParseJoinExpression(newExpression.Arguments[0]);
                }
                else if (lambdaExpression.Body is MemberExpression memberExpression)
                {
                    return ParseJoinExpression(memberExpression);  
                }
                else if (lambdaExpression.Body is MethodCallExpression methodCallExpression)
                {
                    if (methodCallExpression.Method.Name != nameof(SharpSqlEntity.Left)
                     && methodCallExpression.Method.Name != nameof(SharpSqlEntity.Inner))
                    {
                        InvalidJoinException(methodCallExpression.Method);
                    }
                }
                else if (lambdaExpression.Body is NewArrayExpression newArrayExpression)
                {
                    var expressions = new List<Expression>(newArrayExpression.Expressions.Count);

                    for (int i = 0; i < newArrayExpression.Expressions.Count; i++)
                    {
                        // Joins without a call
                        if (newArrayExpression.Expressions[i].Type.IsSubclassOf(typeof(SharpSqlEntity)))
                        {
                            expressions.Add(Expression.Call(newArrayExpression.Expressions[i], typeof(SharpSqlEntity).GetMethod(nameof(SharpSqlEntity.Left))));
                        }
                        // Joins with a call
                        else if (newArrayExpression.Expressions[i] is MethodCallExpression methodCallExpression2)
                        {
                            if (methodCallExpression2.Method.Name != nameof(SharpSqlEntity.Left)
                             && methodCallExpression2.Method.Name != nameof(SharpSqlEntity.Inner))
                            {
                                InvalidJoinException((newArrayExpression.Expressions[i] as MethodCallExpression).Method);
                            }

                            expressions.Add(methodCallExpression2);
                        }
                        // ManyToMany without a call
                        else if (newArrayExpression.Expressions[i].Type.BaseType.GenericTypeArguments.Length > 0)
                        {


                            //foreach (var type in newArrayExpression.Expressions[i].Type.BaseType.GenericTypeArguments)
                            //{
                            //    var result = SharpSqlUtilities.ManyToManyRelations.Keys.FirstOrDefault(x => x.CollectionTypeLeft == SharpSqlUtilities.CollectionEntityRelations[type]);
                            //    var attribute = SharpSqlUtilities.ManyToManyRelations[result];

                            //    var parameterExpression = Expression.Parameter(attribute.CollectionType, "x");




                            //}
                        }
                        else
                        {
                            InvalidJoinException((PropertyInfo)(newArrayExpression.Expressions[i] as MemberExpression).Member);
                        }
                    }

                    return Expression.NewArrayInit(typeof(object), expressions);
                }
                else if (lambdaExpression.Body is UnaryExpression unaryExpression)
                {
                    return ParseJoinExpression(unaryExpression.Operand);
                }
            }
            else if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Type.IsSubclassOf(typeof(SharpSqlEntity)))
                {
                    return Expression.Call(memberExpression, typeof(SharpSqlEntity).GetMethod(nameof(SharpSqlEntity.Left)));
                }

                InvalidJoinException((PropertyInfo)memberExpression.Member);
            }

            return expression;
        }

        internal static string ParseUpdateExpression(Expression body)
        {
            switch (body)
            {
                case MemberExpression memberExpression:
                    {
                        if (memberExpression.Member.GetCustomAttributes(typeof(SharpSqlColumnAttribute), true).FirstOrDefault() is SharpSqlColumnAttribute columnAttribute)
                        {
                            return columnAttribute.ColumnName;
                        }
                        else
                        {
                            return memberExpression.Member.Name;
                        }
                    }
                case UnaryExpression unaryExpression:
                    {
                        return ParseUpdateExpression(unaryExpression.Operand);
                    }
                case LambdaExpression lambdaExpression:
                    {
                        return ParseUpdateExpression(lambdaExpression.Body);
                    }
                default:
                    throw new NotImplementedException(body.NodeType.ToString());
                case null:
                    throw new ArgumentNullException(nameof(body));
            }
        }

        private static void InvalidJoinException(PropertyInfo propertyInfo)
        {
            throw new InvalidJoinException(propertyInfo);
        }

        private static void InvalidJoinException(MethodInfo methodInfo)
        {
            throw new InvalidJoinException(methodInfo);
        }

        private string Where(Expression whereExpression)
        {
            return $" WHERE {ParseExpression(whereExpression)}";
        }

        private void WhereClauseFromEntity(StringBuilder stringBuilder, SharpSqlEntity entity)
        {
            if (!entity.DirtyTracker.AnyDirtyRelations(entity)
              || entity.DirtyTracker.Any)
            {
                stringBuilder.Append(From(new SharpSqlTableAttribute(SharpSqlUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType())));

                var propertyInfo = entity.GetPrimaryKeyPropertyInfo();

                for (int i = 0; i < propertyInfo.Length; i++)
                {
                    var memberExpression = Expression.Property(Expression.Parameter(entity.GetType(), "x"), propertyInfo[i]);
                    var constantExpression = Expression.Constant(propertyInfo[i].GetValue(entity), propertyInfo[i].GetValue(entity).GetType());

                    stringBuilder.Append(Where(Expression.Equal(memberExpression, constantExpression)));
                }

                stringBuilder.Append(Constants.Semicolon);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private string OrderBy(Expression sortExpression)
        {
            return $" ORDER BY {ParseExpression(sortExpression)}";
        }

        private string Update(SharpSqlEntity entity)
        {
            var stringBuilder = new StringBuilder();

            AddQueryTableName(new SharpSqlTableAttribute(SharpSqlUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType()));

            var tableAlias = TableOrder.First(x => x.Type == entity.GetType()).Name;

            if (entity.DirtyTracker.Any
            || !entity.DirtyTracker.AnyDirtyRelations(entity))
            {
                stringBuilder.Append($"UPDATE [{tableAlias}] SET ");
            }

            int entityFieldUpdateCount = 0;
            for (int i = 0; i < entity.MutableTableScheme.Count; i++)
            {
                if (entity.PrimaryKey.Keys.Any(x => x.ColumnName == entity.MutableTableScheme[i] && entity.IsAutoIncrement)
                || !entity.DirtyTracker.IsDirty(entity.MutableTableScheme[i]))
                    continue;

                var fieldPropertyInfo = entity.GetPropertyInfo(entity.MutableTableScheme[i]);

                // Checks if the current entity is a joined entity.
                if (fieldPropertyInfo.GetValue(entity) is SharpSqlEntity entityColumnJoin && fieldPropertyInfo.PropertyType.IsSubclassOf(typeof(SharpSqlEntity)))
                {
                    // Join child-object is new, or one or more fields are dirty.
                    if (entityColumnJoin.IsNew && !entityColumnJoin.DirtyTracker.Any)
                    {
                        AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                    }
                    // Parent entity join field is dirty.
                    else if (entity.DirtyTracker.IsDirty(entity.MutableTableScheme[i]))
                    {
                        AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                    }
                }
                else
                {
                    AddUpdatedParameter(stringBuilder, entity, fieldPropertyInfo, tableAlias, ref entityFieldUpdateCount, i);
                }
            }

            WhereClauseFromEntity(stringBuilder, entity);

            return stringBuilder.ToString();
        }

        private void AddUpdatedParameter(StringBuilder stringBuilder, SharpSqlEntity entity, PropertyInfo propertyInfo, string tableAlias, ref int entityFieldUpdateCount, int currentTableSchemeIndex)
        {
            var addon = ((entity.DirtyTracker.Count <= ++entityFieldUpdateCount) ? " " : ", ");

            if (propertyInfo.GetValue(entity) is SharpSqlEntity entityColumnJoin)
            {
                if (!entity.PrimaryKey.IsCombinedPrimaryKey)
                {
                    stringBuilder.Append($"[{tableAlias}].[{entity.MutableTableScheme[currentTableSchemeIndex]}] = " + AddSqlParameter((entityColumnJoin.PrimaryKey.Keys[0].Value, entityColumnJoin.PrimaryKey.Keys[0].ColumnName)) + addon);
                }
                else
                {
                    // Combined primary key.
                    throw new NotImplementedException();
                }
            }
            else
            {
                stringBuilder.Append($"[{tableAlias}].[{entity.MutableTableScheme[currentTableSchemeIndex]}] = " + AddSqlParameter(entity.SqlValue(entity.MutableTableScheme[currentTableSchemeIndex])) + addon);
            }
        }

        private string Delete(SharpSqlEntity entity)
        {
            AddQueryTableName(new SharpSqlTableAttribute(SharpSqlUtilities.CollectionEntityRelations[entity.GetType()], entity.GetType()));

            var stringBuilder = new StringBuilder("DELETE ");

            WhereClauseFromEntity(stringBuilder, entity);

            return stringBuilder.ToString();
        }

        internal static string Count(SharpSqlTableAttribute tableAttribute)
        {
            return $"SELECT COUNT(*) FROM { tableAttribute.TableName } AS INT;";
        }

        internal static string ColumnConstraintInformation(string tableName)
        {
            return $"SELECT * FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WHERE CONSTRAINT_NAME = 'UC_{ tableName }' AND TABLE_NAME = '{ tableName }'";
        }

        internal static string IfExists(string query)
        {
            return $"IF EXISTS({ query }) BEGIN SELECT 1 END ELSE BEGIN SELECT 0 END;";
        }

        internal static string ServerDatabaseList()
        {
            return "SELECT D.NAME FROM SYS.DATABASES AS D;";
        }

        internal string CreateUniqueConstraint(string tableName, params string[] columnNames)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"ALTER TABLE { tableName } ADD CONSTRAINT UC_{ tableName } UNIQUE(");

            for (int i = 0; i < columnNames.Length; i++)
            {
                var addon = (i <= columnNames.Length) ? string.Empty : ", ";

                stringBuilder.Append($"{ columnNames[i] }{ addon }");
            }

            stringBuilder.Append(");");

            return stringBuilder.ToString();
        }

        internal static string DropUniqueConstraint<EntityType>(EntityType entity) where EntityType : SharpSqlEntity
        {
            string tableName = SharpSqlUtilities.GetTableNameFromEntity(entity);

            return $"ALTER TABLE { tableName } DROP CONSTRAINT UC_{ tableName };";
        }

        private string ParseExpression(Expression body, string source = null)
        {
            switch (body)
            {
                case BinaryExpression binaryExpression:
                    var left = binaryExpression.Left;
                    var right = binaryExpression.Right;

                    return binaryExpression.NodeType switch
                    {
                        ExpressionType.Equal => $"({ParseExpression(left)} = {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.NotEqual => $"({ParseExpression(left)} <> {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.LessThan => $"({ParseExpression(left)} < {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.GreaterThan => $"({ParseExpression(left)} > {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.LessThanOrEqual => $"({ParseExpression(left)} <= {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.GreaterThanOrEqual => $"({ParseExpression(left)} >= {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.Or or ExpressionType.OrElse => $"({ParseExpression(left)} OR {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        ExpressionType.And or ExpressionType.AndAlso => $"({ParseExpression(left)} AND {ParseExpression(right, GetMemberExpressionFromExpression(left))})",
                        _ => throw new NotImplementedException(body.NodeType.ToString()),
                    };
                case MemberExpression memberExpression:
                    {
                        if (ReconstructConstantExpressionFromMemberExpression(memberExpression) is ConstantExpression constantExpression)
                        {
                            return ParseExpression(constantExpression);
                        }

                        var entityType = memberExpression.Member.ReflectedType;
                        var collectionType = SharpSqlUtilities.CollectionEntityRelations[entityType];

                        if (memberExpression.Member.GetCustomAttributes(typeof(SharpSqlColumnAttribute), true).FirstOrDefault() is SharpSqlColumnAttribute columnAttribute)
                        {
                            return $"[{_queryTableNames[collectionType.Name]}].[{columnAttribute.ColumnName}]";
                        }
                        else
                        {
                            return $"[{_queryTableNames[collectionType.Name]}].[{memberExpression.Member.Name}]";
                        }
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
                            // SharpSqlEntityExtensions.Contains
                            nameof(string.Contains) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Constants.QueryParam + SqlParameters.Count} + '%')",
                            // SharpSqlEntityExtensions.StartsWith
                            nameof(string.StartsWith) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE {Constants.QueryParam + SqlParameters.Count} + '%')",
                            // SharpSqlEntityExtensions.EndsWith
                            nameof(string.EndsWith) => $"({ParseExpression(methodCallExpression?.Object ?? methodCallExpression.Arguments.OfType<MemberExpression>().FirstOrDefault())} LIKE '%' + {Constants.QueryParam + SqlParameters.Count})",
                            nameof(string.ToString) => ParseExpression(methodCallExpression.Object),
                            nameof(SharpSqlExtensions.Ascending) => $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} {Constants.OrderByAsc}",
                            nameof(SharpSqlExtensions.Descending) => $"{ParseExpression(methodCallExpression.Arguments.FirstOrDefault() ?? throw new InvalidOperationException($"No field for lambda expression [{(methodCallExpression.Object as ParameterExpression).Name}]."))} {Constants.OrderByDesc}",
                            nameof(SharpSqlEntity.Left) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, Constants.Left),
                            nameof(SharpSqlEntity.Inner) => GenerateJoinQuery(methodCallExpression.Object as MemberExpression, Constants.Inner),
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
                case NewExpression newExpression:
                    {
                        var query = string.Empty;

                        for (int i = 0; i < newExpression.Arguments.Count; i++)
                        {
                            if (newExpression.Arguments[i] is MemberExpression memberExpression)
                            {
                                var addon = (newExpression.Arguments.Count - 1 == i) ? string.Empty : ", ";

                                query += $"{ ParseExpression(newExpression.Arguments[i]) }{ addon }";

                                continue;
                            }

                            throw new NotImplementedException();
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
            SqlParameters.Add(new SqlParameter(Constants.QueryParam + (SqlParameters.Count + 1), mappedValue.value)
            {
                SourceColumn = mappedValue.sourceColumn
            });

            return SqlParameters[^1].ParameterName;
        }

        private void AddQueryTableName(SharpSqlTableAttribute table)
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

        private RelationalJoin CalculateJoins(SharpSqlTableAttribute tableAttribute, string tableName)
        {
            var propertyInfo = tableAttribute.EntityType.GetProperty(tableName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            var rightTableAttribute = SharpSqlUtilities.CollectionEntityRelations[propertyInfo.PropertyType].GetCustomAttributes(typeof(SharpSqlTableAttribute), true).First() as SharpSqlTableAttribute;
            AddQueryTableName(rightTableAttribute);

            var rightInstance = (SharpSqlEntity)Activator.CreateInstance(propertyInfo.PropertyType);

            RelationalJoin join = new()
            {
                LeftTableAttribute = tableAttribute,
                LeftPropertyInfo = propertyInfo,
                RightTableAttribute = rightTableAttribute,
                RightPropertyInfo = rightInstance.GetPrimaryKeyPropertyInfo()
            };

            // Lookup parent path if available and add this current path to the list
            var parentTableName = _queryTableNames[tableAttribute.TableName];
            var basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : string.Empty;
            TableNameResolvePaths.Add(_queryTableNames[rightTableAttribute.TableName], basePath + propertyInfo.Name());

            return join;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GenerateJoinQuery(MemberExpression expression, string joinType)
        {
            var targetProperty = expression.Member.DeclaringType.GetProperty(expression.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            var manyToManyRelations = SharpSqlUtilities.ManyToManyRelations.GetValueOrDefault((SharpSqlUtilities.CollectionEntityRelations[expression.Member.DeclaringType], targetProperty.PropertyType));
            var stringBuilder = new StringBuilder();

            if (manyToManyRelations != null)
            {
                ContainsToManyJoins = true;

                var properties = manyToManyRelations.EntityType.GetProperties();

                RelationalJoin firstJoin = new()
                {
                    IsManyToMany = true,
                    LeftTableAttribute = TableAttribute,
                    LeftPropertyInfo = TableAttribute.EntityType.GetProperties().Where(x => (x.GetCustomAttributes(typeof(SharpSqlPrimaryKeyAttribute), true).FirstOrDefault() as SharpSqlPrimaryKeyAttribute) != null).First(),
                    RightTableAttribute = manyToManyRelations.CollectionType.GetCustomAttribute<SharpSqlTableAttribute>(),
                    RightPropertyInfo = properties.Where(x => (x.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), true).FirstOrDefault() as SharpSqlForeignKeyAttribute)?.Relation == expression.Member.DeclaringType).ToArray()
                };

                if (firstJoin.RightPropertyInfo.Length == 0)
                {
                    var propertyInfo = properties.Where(x => (x.GetCustomAttributes(typeof(SharpSqlPrimaryKeyAttribute), true).FirstOrDefault() as SharpSqlPrimaryKeyAttribute) != null
                                                    && (x.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), true).FirstOrDefault() as SharpSqlForeignKeyAttribute) == null)
                                                       .FirstOrDefault();

                    throw new ForeignKeyAttributeNotImplementedException(propertyInfo, firstJoin.RightTableAttribute.CollectionType);
                }

                RelationalJoin secondJoin = new()
                {
                    IsManyToMany = true,
                    LeftTableAttribute = manyToManyRelations.CollectionType.GetCustomAttribute<SharpSqlTableAttribute>(),
                    LeftPropertyInfo = properties.Where(x => (x.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), true).FirstOrDefault() as SharpSqlForeignKeyAttribute)?.Relation == SharpSqlUtilities.CollectionEntityRelations[targetProperty.PropertyType]).FirstOrDefault(),
                    RightTableAttribute = targetProperty.PropertyType.GetCustomAttribute<SharpSqlTableAttribute>(),
                    RightPropertyInfo = SharpSqlUtilities.CollectionEntityRelations[targetProperty.PropertyType].GetProperties().Where(x => (x.GetCustomAttributes(typeof(SharpSqlPrimaryKeyAttribute), true).FirstOrDefault() as SharpSqlPrimaryKeyAttribute) != null).ToArray()
                };

                if (secondJoin.LeftPropertyInfo == null)
                {
                    var propertyInfo = SharpSqlUtilities.CollectionEntityRelations[targetProperty.PropertyType].GetProperties()
                                                   .Where(x => (x.GetCustomAttributes(typeof(SharpSqlPrimaryKeyAttribute), true).FirstOrDefault() as SharpSqlPrimaryKeyAttribute) != null
                                                      && (x.GetCustomAttributes(typeof(SharpSqlForeignKeyAttribute), true).FirstOrDefault() as SharpSqlForeignKeyAttribute) == null)
                                                         .FirstOrDefault();

                    throw new ForeignKeyAttributeNotImplementedException(propertyInfo, secondJoin.LeftTableAttribute.CollectionType);
                }

                AddQueryTableName(firstJoin.RightTableAttribute);
                AddQueryTableName(secondJoin.RightTableAttribute);

                Joins.Add(firstJoin);
                Joins.Add(secondJoin);

                GenerateJoinSql(joinType, stringBuilder, firstJoin);
                GenerateJoinSql(joinType, stringBuilder, secondJoin);

                var parentTableName = _queryTableNames[firstJoin.RightTableAttribute.TableName];
                var basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}" : string.Empty;
                TableNameResolvePaths.Add(_queryTableNames[firstJoin.RightTableAttribute.TableName], basePath + targetProperty.Name());

                parentTableName = _queryTableNames[secondJoin.RightTableAttribute.TableName];
                basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}" : string.Empty;
                TableNameResolvePaths.Add(_queryTableNames[secondJoin.RightTableAttribute.TableName], basePath + targetProperty.Name());

                return stringBuilder.ToString();
            }

            var join = CalculateJoins(TableAttribute, expression.Member.Name);
            Joins.Add(join);
            GenerateJoinSql(joinType, stringBuilder, join);
            return stringBuilder.ToString();
        }

        private void GenerateJoinSql(string joinType, StringBuilder stringBuilder, RelationalJoin join)
        {
            for (int i = 0; i < join.RightPropertyInfo.Length; i++)
            {
                stringBuilder.Append($" {joinType} JOIN [DBO].[{join.RightTableAttribute.TableName}] AS [{_queryTableNames[join.RightTableAttribute.TableName]}] ON [{_queryTableNames[join.LeftTableAttribute.TableName]}].[{join.LeftPropertyInfo.Name()}] = [{_queryTableNames[join.RightTableAttribute.TableName]}].[{join.RightPropertyInfo[i].Name()}]");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsArrayExpressionOfTypeJoin(NewArrayExpression newArrayExpression)
        {
            return newArrayExpression.Expressions
                                     .OfType<MethodCallExpression>()
                                     .Any(x => x.Method.Name == nameof(SharpSqlEntity.Left)
                                            || x.Method.Name == nameof(SharpSqlEntity.Inner));
        }

        private string GetMemberExpressionFromExpression(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                return GetMemberExpressionFromExpression(methodCallExpression.Object);
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                return GetMemberExpressionFromExpression(binaryExpression.Left);
            }
            else
            {
                throw new NotImplementedException(expression.GetType().FullName);
            }
        }

        private ConstantExpression ReconstructConstantExpressionFromMemberExpression(MemberExpression memberExpression)
        {
            // Local variables
            if (memberExpression.Expression is ConstantExpression constantExpression)
            {
                var value = GetValue(memberExpression.Member, constantExpression.Value);

                return Expression.Constant(value, value.GetType());
            }
            // Local properties
            else if (memberExpression.Expression is MemberExpression subMemberExpression && !subMemberExpression.Member.DeclaringType.IsSubclassOf(typeof(SharpSqlEntity)))
            {
                var value = GetValue(memberExpression.Member, ReconstructConstantExpressionFromMemberExpression(subMemberExpression).Value);

                return Expression.Constant(value, value.GetType());
            }

            return null;
        }

        private static object GetValue(MemberInfo member, object instance)
        {
            if (member is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(instance, null);
            }
            if (member is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(instance);
            }

            throw new InvalidOperationException();
        }
    }
}