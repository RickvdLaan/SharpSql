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
        private string _generatedQuery = null;

        private List<object> _sqlParameters = new List<object>(10);

        internal List<SQLClause> SQLClauses { get; set; }

        public SqlParameter[] SqlParameters { get; private set; }

        public SQLBuilder()
        {
            SQLClauses = new List<SQLClause>();
        }

        public override string ToString()
        {
            return _generatedQuery;
        }

        internal void AddSQLClause(SQLClause clause)
        {
            AddSQLClauses(clause);
        }

        internal void AddSQLClauses(params SQLClause[] clauses)
        {
            SQLClauses.AddRange(clauses);
        }

        internal void BuildQuery(ORMTableAttribute tableAttribute, long maxNumberOfItemsToReturn)
        {
            StringBuilder stringBuilder = new StringBuilder();
            SQLClauseBuilderBase clauseBuilder = new SQLClauseBuilderBase();

            AddSQLClauses(
                clauseBuilder.Select(maxNumberOfItemsToReturn),
                clauseBuilder.From(tableAttribute.TableName),
                clauseBuilder.Semicolon());

            SQLClause select = SQLClauses.Where(x => x.Type == SQLClauseType.Select).First();
            SQLClause from = SQLClauses.Where(x => x.Type == SQLClauseType.From).First();
            SQLClause semicolon = SQLClauses.Where(x => x.Type == SQLClauseType.Semicolon).First();

            stringBuilder.Append(select.Sql);
            stringBuilder.Append(from.Sql);
            stringBuilder.Append(semicolon.Sql);

            _generatedQuery = stringBuilder.ToString();
        }

        internal void BuildQuery(Expression body, ORMTableAttribute tableAttribute, long maxNumberOfItemsToReturn)
        {
            var parsedExpression = ParseExpression(body);
            GenerateSqlParameters();

            StringBuilder stringBuilder = new StringBuilder();
            SQLClauseBuilderBase clauseBuilder = new SQLClauseBuilderBase();

            AddSQLClauses(
                clauseBuilder.Select(maxNumberOfItemsToReturn),
                clauseBuilder.From(tableAttribute.TableName),
                new SQLClause(parsedExpression, SQLClauseType.Where, SqlParameters),
                clauseBuilder.Semicolon());

            SQLClause select = SQLClauses.Where(x => x.Type == SQLClauseType.Select).First();
            SQLClause from = SQLClauses.Where(x => x.Type == SQLClauseType.From).First();
            List<SQLClause> where = SQLClauses.Where(x => x.Type == SQLClauseType.Where).ToList();
            SQLClause semicolon = SQLClauses.Where(x => x.Type == SQLClauseType.Semicolon).First();

            stringBuilder.Append(select.Sql);
            stringBuilder.Append(from.Sql);

            var tempList = new List<SqlParameter>();

            if (where.Any())
            {
                SQLClause clause = where.First();

                stringBuilder.Append($"WHERE ({clause.Sql})");
                tempList.AddRange(clause.Parameters.ToList());
            }

            stringBuilder.Append(semicolon.Sql);

            _generatedQuery = stringBuilder.ToString();
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
                        var type = body as MemberExpression;

                        return type.Member.Name;
                    }
                case ExpressionType.Constant:
                    {
                        var type = body as ConstantExpression;

                        _sqlParameters.Add(type.Value);

                        return $"@param{_sqlParameters.Count}";
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private void GenerateSqlParameters()
        {
            SqlParameters = new SqlParameter[_sqlParameters.Count];

            for (int i = 0; i < _sqlParameters.Count; i++)
            {
                string paramName = $"@param{i + 1}";
                SqlParameters[i] = (new SqlParameter(paramName, _sqlParameters[i]));
            }
        }
    }
}