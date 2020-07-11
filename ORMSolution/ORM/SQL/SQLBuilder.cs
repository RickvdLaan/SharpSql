using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ORM
{
    internal class SQLBuilder
    {
        private const string Param = "@PARAM";

        private readonly List<object> _sqlParameters = new List<object>(10);

        private readonly Dictionary<string, string> _queryTableNames = new Dictionary<string, string>(5);

        private readonly Dictionary<char, int> _tableCharCounts = new Dictionary<char, int>(5);

        private readonly List<Join> Joins = new List<Join>();

        public readonly Dictionary<string, string> TableNameResolvePaths = new Dictionary<string, string>();

        public string GeneratedQuery { get; private set; }

        internal SqlParameter[] SqlParameters { get; private set; }

        internal ORMTableAttribute TableAttribute { get; set; }

        public override string ToString()
        {
            return GeneratedQuery;
        }

        public void BuildQuery(ORMTableAttribute tableAttribute, Expression selectExpression, Expression whereExpression, Expression sortExpression, bool includeSubObjects, long maxNumberOfItemsToReturn)
        {
            TableAttribute = tableAttribute;

            AddQueryTableName(TableAttribute.TableName);

            if (includeSubObjects)
            {
                CalculateJoins(tableAttribute);
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(Select(selectExpression, maxNumberOfItemsToReturn));
            stringBuilder.Append(From());

            if (Joins.Any())
            {
                foreach (var join in Joins)
                {
                    stringBuilder.Append(Join(join));
                }
            }

            if (whereExpression != null)
            {
                stringBuilder.Append(Where(whereExpression));
            }
            if (sortExpression != null)
            {
                stringBuilder.Append(OrderBy(sortExpression));
            }

            stringBuilder.Append(Semicolon());

            GeneratedQuery = stringBuilder.ToString().ToUpperInvariant();
        }

        private void CalculateJoins(ORMTableAttribute tableAttribute)
        {
            var properties = tableAttribute.EntityType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var subObjects = properties.Where(x => x.PropertyType.IsSubclassOf(typeof(ORMEntity))).ToList();

            foreach (var lProperty in subObjects)
            {
                var rTableAttr = ORMUtilities.CollectionEntityRelations[lProperty.PropertyType].GetCustomAttributes(typeof(ORMTableAttribute), true).First() as ORMTableAttribute;
                AddQueryTableName(rTableAttr.TableName);

                var rightInstance = (ORMEntity)Activator.CreateInstance(lProperty.PropertyType);

                Joins.Add(new Join()
                {
                    type = JoinType.Left,
                    lTableAttr = tableAttribute,
                    lProperty = lProperty,
                    rTableAttr = rTableAttr,
                    rProperty = rightInstance.GetPrimaryKeyPropertyInfo()
                });
                // Lookup parent path if available and add this current path to the list
                var parentTableName = _queryTableNames[tableAttribute.TableName];
                var basePath = TableNameResolvePaths.ContainsKey(parentTableName) ? $"{TableNameResolvePaths[parentTableName]}." : "";
                TableNameResolvePaths.Add(_queryTableNames[rTableAttr.TableName], lProperty.Name);

                // Check if more joins are required for subobjects
                CalculateJoins(rTableAttr);
            }
        }

        private char Semicolon()
        {
            return ';';
        }

        private string Select((string tableName, string propertyName)[] properties, long top = -1)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("SELECT");
            if (top >= 0)
            {
                stringBuilder.Append($" TOP ({top})");
            }
            
            bool isFirst = true;
            foreach(var property in properties)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    stringBuilder.Append(',');
                }
                stringBuilder.Append($" [{property.tableName}].[{property.propertyName}] AS [{property.tableName}.{property.propertyName}]");
            }

            return stringBuilder.ToString();
        }

        private string Select(Expression selectExpression, long top = -1)
        {
            var properties = GetAllSelectPropertyStringsOfType(TableAttribute);

            if (Joins.Any())
            {
                var rightTables = Joins.Select(x => x.rTableAttr).Distinct().ToList();
                var joinProperties = rightTables.Select(GetAllSelectPropertyStringsOfType).ToList();
                joinProperties.Add(properties);

                properties = ORMUtilities.ConcatArrays(joinProperties.ToArray());
            }

            if (selectExpression != null)
            {
                var expressionResult = ParseExpression(selectExpression).Split(',');
                properties = properties.Where(x => expressionResult.Contains($"[{x.tableName}].[{x.propertyName}]")).ToArray();
            }

            return Select(properties, top);
        }

        private (string tableName, string propertyName)[] GetAllSelectPropertyStringsOfType(ORMTableAttribute table)
        {
            var properties = table.EntityType.GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                .Where(x => !x.PropertyType.IsSubclassOf(typeof(ORMEntity)) && x.Name != nameof(ORMEntity.ExecutedQuery)).ToArray();

            var result = new (string tableName, string propertyName)[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                result[i] = (_queryTableNames[table.TableName], properties[i].Name);
            }

            return result;
        }

        private string From()
        {
            return $" FROM [dbo].[{TableAttribute.TableName}] AS [{_queryTableNames[TableAttribute.TableName]}]";
        }

        private string Join(Join join)
        {
            string type;
            switch (join.type)
            {
                case JoinType.Inner:
                    type = "INNER JOIN";
                    break;
                case JoinType.Left:
                    type = "LEFT JOIN";
                    break;
                case JoinType.Right:
                    type = "RIGHT JOIN";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return $" {type} [dbo].[{join.rTableAttr.TableName}] AS [{_queryTableNames[join.rTableAttr.TableName]}] ON [{_queryTableNames[join.lTableAttr.TableName]}].[{join.lProperty.Name}] = [{_queryTableNames[join.rTableAttr.TableName]}].[{join.rProperty.Name}]";
        }

        private string Where(Expression whereExpression)
        {
            return $" WHERE {ParseWhereExpression(whereExpression)}";
        }

        private string OrderBy(Expression sortExpression)
        {
            return $" ORDER BY {ParseExpression(sortExpression)}";
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

        private string ParseExpression(Expression body)
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
                            var field = $"{ParseExpression(newArrayExpression.Expressions[i])}";
                            var addon = ((newArrayExpression.Expressions.Count - 1 == i) ? string.Empty : ", ");
                            query += $"{field}{addon}";
                        }

                        return query;
                    }
                default:
                    throw new NotImplementedException(body.NodeType.ToString());
                case null:
                    throw new ArgumentNullException(nameof(body));
            }
        }

        private void AddQueryTableName(string tableName)
        {
            char firstChar = tableName[0];

            if (_tableCharCounts.ContainsKey(firstChar))
            {
                _tableCharCounts[firstChar] = ++_tableCharCounts[firstChar];
            }
            else
            {
                _tableCharCounts[firstChar] = 1;
            }

            _queryTableNames[tableName] = new string(firstChar, _tableCharCounts[firstChar]);
        }
    }
}