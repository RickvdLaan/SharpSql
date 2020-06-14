namespace ORM
{
    public struct SQLClause
    {
        internal SQLClauseType Type { get; set; }
        internal string Sql { get; set; }
        internal object[] Parameters { get; set; }

        public SQLClause(string sql, SQLClauseType type, object[] parameters = null)
        {
            Sql = sql;
            Parameters = parameters;
            Type = type;
        }
    }
}
