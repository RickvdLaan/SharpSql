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
                AddSQLClause(SQLClauseBuilderBase.OrderBy(sortExpression));
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
            switch (body.NodeType)
            {
                case ExpressionType.Equal:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left;
                        var right = type.Right;

                        return $"({ParseExpression(left)} = {ParseExpression(right)})";
                    }
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left;
                        var right = type.Right;

                        return $"({ParseExpression(left)} OR {ParseExpression(right)})";
                    }
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left;
                        var right = type.Right;

                        return $"({ParseExpression(left)} AND {ParseExpression(right)})";
                    }
                case ExpressionType.MemberAccess:
                    {
                        var memberExpressionMember = (body as MemberExpression).Member;
                        return $"{SQLClauseBuilderBase.QueryTableNames[ORMUtilities.EntityTypes[memberExpressionMember.ReflectedType].Name]}.[{memberExpressionMember.Name}]";
                    }
                case ExpressionType.Constant:
                    {
                        _sqlParameters.Add((body as ConstantExpression).Value);

                        return $"{Param + _sqlParameters.Count}";
                    }
                case ExpressionType.Call:
                    {
                        var type = body as MethodCallExpression;
                        if (type.Arguments.FirstOrDefault() != null)
                        {
                            _sqlParameters.Add((type.Arguments.First() as ConstantExpression).Value);
                        }
                        switch (type.Method.Name)
                        {
                            case nameof(string.Contains):
                                return $"({ParseExpression(type.Object)} LIKE '%' + {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.StartsWith):
                                return $"({ParseExpression(type.Object)} LIKE {Param + _sqlParameters.Count} + '%')";
                            case nameof(string.EndsWith):
                                return $"({ParseExpression(type.Object)} LIKE '%' + {Param + _sqlParameters.Count})";
                            case nameof(string.ToString):
                                return ParseExpression(type.Object);
                            default:
                                throw new NotImplementedException(type.Method.Name);
                        }
                    }
                default:
                    throw new NotImplementedException();
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