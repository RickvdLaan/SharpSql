using Microsoft.Data.SqlClient;

namespace ORM
{
    internal struct SQLClause
    {
        internal SQLClauseType Type { get; set; }
        internal string Sql { get; set; }
        internal SqlParameter[] Parameters { get; set; }

        public SQLClause(string sql, SQLClauseType type, params SqlParameter[] sqlParameters)
        {
            Sql = sql;
            Type = type;
            Parameters = sqlParameters;
        }
    }
}
