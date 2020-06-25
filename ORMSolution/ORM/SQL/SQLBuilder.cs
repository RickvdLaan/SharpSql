using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ORM
{
    internal class SQLBuilder
    {
        private string GeneratedQuery { get; set; }
        
        public SqlParameter[] SqlParameters { get; private set; }

        internal List<SQLClause> SQLClauses { get; set; }

        internal SQLClauseBuilderBase SQLClauseBuilderBase { get; set; }

        private readonly List<object> _sqlParameters = new List<object>(10);

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

        internal void BuildQuery(ORMTableAttribute tableAttribute, ORMEntityField[] selectExpression, ORMSortExpression sortExpression, long maxNumberOfItemsToReturn)
        {
            BuildQuery(tableAttribute, selectExpression, sortExpression, maxNumberOfItemsToReturn, null);
        }

        internal void BuildQuery(ORMTableAttribute tableAttribute, ORMEntityField[] selectExpression, Expression whereExpression, ORMSortExpression sortExpression, long maxNumberOfItemsToReturn)
        {
            BuildQuery(tableAttribute, selectExpression, sortExpression, maxNumberOfItemsToReturn, SQLClauseBuilderBase.Where(ParseExpression, whereExpression, GenerateSqlParameters));
        }

        private void BuildQuery(ORMTableAttribute tableAttribute, ORMEntityField[] selectExpression, ORMSortExpression sortExpression, long maxNumberOfItemsToReturn, params SQLClause[] clauses)
        {
            AddSQLClauses(SQLClauseBuilderBase.Select(selectExpression, maxNumberOfItemsToReturn),
                          SQLClauseBuilderBase.From(tableAttribute.TableName));

            for (int i = 0; i < clauses?.Length; i++)
            {
                AddSQLClause(clauses[i]);
            }

            if (sortExpression.HasSorters)
            {
                AddSQLClause(SQLClauseBuilderBase.OrderBy(sortExpression));
            }

            AddSQLClause(SQLClauseBuilderBase.Semicolon());

            var stringBuilder = new StringBuilder();

            foreach (SQLClause sqlClause in SQLClauses)
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
                        var left = type.Left as MemberExpression;
                        var right = type.Right as ConstantExpression;

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
                        return (body as MemberExpression).Member.Name;
                    }
                case ExpressionType.Constant:
                    {
                        _sqlParameters.Add((body as ConstantExpression).Value);

                        return $"@PARAM{_sqlParameters.Count}";
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
                string parameterName = $"@PARAM{i + 1}";
                SqlParameters[i] = new SqlParameter(parameterName, _sqlParameters[i]);
            }

            return SqlParameters;
        }
    }
}