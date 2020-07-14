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

        private readonly List<object> _sqlParameters = new List<object>(10);
        
        public readonly Dictionary<string, string> _queryTableNames = new Dictionary<string, string>(5);

        private readonly Dictionary<char, int> _tableCharCounts = new Dictionary<char, int>(5);

        private readonly List<SQLJoin> Joins = new List<SQLJoin>();

        public readonly List<(string name, Type type)> TableOrder = new List<(string name, Type type)>(10);
        public readonly Dictionary<string, int> TableNameColumnCount = new Dictionary<string, int>();
        public readonly Dictionary<string, string> TableNameResolvePaths = new Dictionary<string, string>();

        public string GeneratedQuery { get; private set; }

        internal SqlParameter[] SqlParameters { get; private set; }

        internal ORMTableAttribute TableAttribute { get; set; }

        public override string ToString()
        {
            return GeneratedQuery;
        }

        public void BuildQuery(ORMTableAttribute tableAttribute, Expression selectExpression, Expression joinExpression, Expression whereExpression, Expression sortExpression, long maxNumberOfItemsToReturn)
        {
            TableAttribute = tableAttribute;

            AddQueryTableName(TableAttribute);

            var stringBuilder = new StringBuilder();

            // Select is prepended at the end to calculate table counts
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

            stringBuilder.Append(Semicolon());

            stringBuilder.Insert(0, Select(selectExpression, maxNumberOfItemsToReturn));

            GeneratedQuery = stringBuilder.ToString().ToUpperInvariant();
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
                            case nameof(ORMEntityExtensions.Left):
                                return GenerateJoinQuery(methodCallExpression.Arguments.First() as MemberExpression, "LEFT");
                            case nameof(ORMEntityExtensions.Right):
                                return GenerateJoinQuery(methodCallExpression.Arguments.First() as MemberExpression, "RIGHT");
                            case nameof(ORMEntityExtensions.Inner):
                                return GenerateJoinQuery(methodCallExpression.Arguments.First() as MemberExpression, "INNER");
                            case nameof(ORMEntityExtensions.Full):
                                return GenerateJoinQuery(methodCallExpression.Arguments.First() as MemberExpression, "FULL");
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
            var join = CalculateJoins(TableAttribute, expression.Member.Name);
            Joins.Add(join);

            return $" {joinType} JOIN [dbo].[{join.RightTableAttribute.TableName}] AS [{_queryTableNames[join.RightTableAttribute.TableName]}] ON [{_queryTableNames[join.LeftTableAttribute.TableName]}].[{join.LeftPropertyInfo.Name}] = [{_queryTableNames[join.RightTableAttribute.TableName]}].[{join.RightPropertyInfo.Name}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsArrayExpressionOfTypeJoin(NewArrayExpression newArrayExpression)
        {
            return newArrayExpression.Expressions
                                     .OfType<MethodCallExpression>()
                                     .Any(x => x.Method.Name == nameof(ORMEntityExtensions.Left)
                                            || x.Method.Name == nameof(ORMEntityExtensions.Right)
                                            || x.Method.Name == nameof(ORMEntityExtensions.Inner)
                                            || x.Method.Name == nameof(ORMEntityExtensions.Full));
        }
    }
}