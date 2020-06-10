namespace ORM
{
    internal struct SQLClause
    {
        internal string Sql { get; set; }
        internal object[] Parameters { get; set; }

        public SQLClause(string sql, object[] parameters = null)
        {
            Sql = sql;
            Parameters = parameters;
        }
    }
}
