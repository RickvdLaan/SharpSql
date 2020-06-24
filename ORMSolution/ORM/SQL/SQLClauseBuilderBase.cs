using Microsoft.Data.SqlClient;
using System;
using System.Linq.Expressions;

namespace ORM
{
    internal class SQLClauseBuilderBase
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

        public SQLClause OrderBy(ORMSortExpression sortExpression)
        {
            var query = " ORDER BY ";

            for (int i = 0; i < sortExpression.Sorters.Count; i++)
            {
                var column = sortExpression.Sorters[i].Column;
                var orderBy = sortExpression.Sorters[i].OrderBy.Description();
                var addon = ((sortExpression.Sorters.Count - 1 == i) ? string.Empty : ", ");

                query += $"{column} {orderBy}{addon}";
            }

            return new SQLClause(query, SQLClauseType.OrderBy);
        }

        public SQLClause Semicolon()
        {
            return new SQLClause(";", SQLClauseType.Semicolon);
        }
    }
}
