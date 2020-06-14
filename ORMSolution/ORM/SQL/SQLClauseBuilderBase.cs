namespace ORM
{
    public class SQLClauseBuilderBase
    {
        public SQLClause From(string tableName)
        {
            return new SQLClause(string.Format("from {0} ", tableName), SQLClauseType.From);
        }

        public SQLClause Select(long top = -1)
        {
            return new SQLClause(string.Format("select {0}* ", top >= 0 ? $"top { top } " : string.Empty), SQLClauseType.Select);
        }

        public SQLClause Semicolon()
        {
            return new SQLClause(";", SQLClauseType.Semicolon);
        }
    }
}
