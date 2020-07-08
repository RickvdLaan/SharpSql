using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ORM
{
    internal class SQLBuilder
    {
        private const string Param = "@PARAM";

        private readonly List<object> _sqlParameters = new List<object>(10);

        private string GeneratedQuery { get; set; }
        
        public SqlParameter[] SqlParameters { get; private set; }

        internal List<SQLClause> SQLClauses { get; set; }

        internal SQLClauseBuilderBase SQLClauseBuilderBase { get; set; }

        public SQLBuilder()
        {
            SQLClauses = new List<SQLClause>();
            SQLClauseBuilderBase = new SQLClauseBuilderBase();
        }

        public override string ToString()
        {
            return GeneratedQuery.ToUpper();
        }

        internal void AddSQLClause(SQLClause clause)
        {
            AddSQLClauses(clause);
        }

        internal void AddSQLClauses(params SQLClause[] clauses)
        {
            SQLClauses.AddRange(clauses);
        }

        public void BuildQuery(ORMTableAttribute tableAttribute, ORMEntityField[] selectExpression, Expression whereExpression, ORMSortExpression sortExpression, long maxNumberOfItemsToReturn)
        {
            AddSQLClauses(SQLClauseBuilderBase.Select(selectExpression, maxNumberOfItemsToReturn),
                          SQLClauseBuilderBase.From(tableAttribute.TableName));

            if (whereExpression != null)
            {
                AddSQLClause(SQLClauseBuilderBase.Where(ParseExpression, whereExpression, GenerateSqlParameters));
            }

            if (sortExpression.HasSorters)
            {
                AddSQLClause(SQLClauseBuilderBase.OrderBy(tableAttribute.TableName, sortExpression));
            }

            AddSQLClause(SQLClauseBuilderBase.Semicolon());

            var stringBuilder = new StringBuilder();

            foreach (var sqlClause in SQLClauses)
            {
                stringBuilder.Append(sqlClause.Sql);
            }

            GeneratedQuery = stringBuilder.ToString();
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

                        return $"[{SQLClauseBuilderBase.QueryTableNames[collectionType.Name]}].[{memberExpression.Member.Name}]";
                    }
                case ConstantExpression constantExpression:
                    {
                        _sqlParameters.Add(constantExpression.Value);

                        return $"{Param + _sqlParameters.Count}";
                    }
                case MethodCallExpression methodCallExpression:
                    {
                        if (methodCallExpression.Arguments.FirstOrDefault() != null)
                        {
                            ParseExpression(methodCallExpression.Arguments.First());
                        }
                        switch (methodCallExpression.Method.Name)
                        {
                            case nameof(string.Contains):
                                return $"({ParseExpression(methodCallExpression.Object)} LIKE '%' + {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.StartsWith):
                                return $"({ParseExpression(methodCallExpression.Object)} LIKE {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.EndsWith):
                                return $"({ParseExpression(methodCallExpression.Object)} LIKE '%' + {Param + _sqlParameters.Count})";
                            case nameof(string.ToString):
                                return ParseExpression(methodCallExpression.Object);
                            default:
                                throw new NotImplementedException(methodCallExpression.Method.Name);
                        }
                    }
                default:
                    throw new NotImplementedException(body.NodeType.ToString());
                case null:
                    throw new ArgumentNullException(nameof(body));
            }
        }

        private SqlParameter[] GenerateSqlParameters()
        {
            SqlParameters = new SqlParameter[_sqlParameters.Count];

            for (int i = 0; i < _sqlParameters.Count; i++)
            {
                SqlParameters[i] = new SqlParameter(Param + (i + 1), _sqlParameters[i]);
            }

            return SqlParameters;
        }
    }
}