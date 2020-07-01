using Microsoft.Data.SqlClient;
using System;
using System.Linq.Expressions;

namespace ORM
{
    internal class SQLClauseBuilderBase
    {
        public SQLClause From(string tableName)
        {
            return new SQLClause($"FROM [dbo].[{tableName}]", SQLClauseType.From);
        }

        public SQLClause Select(long top = -1)
        {
            return new SQLClause(top >= 0 ? $"SELECT TOP ({top}) * " : "SELECT * ", SQLClauseType.Select);
        }

        public SQLClause Select(ORMEntityField[] selectExpression, long top = -1)
        {
            if (selectExpression == null)
            {
                return Select(top);
            }

            var fields = string.Empty;

            for (int i = 0; i < selectExpression.Length; i++)
            {
                var field = $"[{selectExpression[i].Name}]";
                var addon = ((selectExpression.Length - 1 == i) ? string.Empty : ", ");
                fields += field + addon;
            }

            return new SQLClause(top >= 0 ? $"SELECT TOP ({top}) {fields} " : $"SELECT {fields} ", SQLClauseType.Select);
        }

        public SQLClause Where(Func<Expression, string> parseExpression, Expression whereExpression, Func<SqlParameter[]> generateSqlParameters)
        {
            var query = $" WHERE {parseExpression.Invoke(whereExpression)}";
            return new SQLClause(query, SQLClauseType.Where, generateSqlParameters.Invoke());
        }

        public SQLClause OrderBy(ORMSortExpression sortExpression)
        {
            var query = " ORDER BY ";

            for (int i = 0; i < sortExpression.Sorters.Count; i++)
            {
                var field = sortExpression.Sorters[i].Field.Name;
                var sortType = sortExpression.Sorters[i].SortType.SQL();
                var addon = ((sortExpression.Sorters.Count - 1 == i) ? string.Empty : ", ");

                query += $"{field} {sortType}{addon}";
            }

            return new SQLClause(query, SQLClauseType.OrderBy);
        }

        public SQLClause Semicolon()
        {
            return new SQLClause(";", SQLClauseType.Semicolon);
        }
    }
}
