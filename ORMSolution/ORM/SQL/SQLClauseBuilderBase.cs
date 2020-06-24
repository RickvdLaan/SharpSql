using Microsoft.Data.SqlClient;
using System;
using System.Linq.Expressions;

namespace ORM
{
    public class SQLClauseBuilderBase
    {
        public SQLClause From(string tableName)
        {
            return new SQLClause(string.Format("FROM {0}", tableName), SQLClauseType.From);
        }

        public SQLClause Select(long top = -1)
        {
            return new SQLClause(top >= 0 ? $"SELECT TOP {top} * " : "SELECT * ", SQLClauseType.Select);
        }

        public SQLClause Where(Func<Expression, string> parseExpression, Expression body, Func<SqlParameter[]> generateSqlParameters)
        {
            var query = $" WHERE ({parseExpression.Invoke(body)})";

            return new SQLClause(query, SQLClauseType.Where, generateSqlParameters.Invoke());
        }

        public SQLClause Semicolon()
        {
            return new SQLClause(";", SQLClauseType.Semicolon);
        }
    }
}
