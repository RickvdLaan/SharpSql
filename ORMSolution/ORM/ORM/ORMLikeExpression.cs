using ORM.Interfaces;
using System.Collections.Generic;

namespace ORM
{
    public sealed class ORMLikeExpression
    {
        internal List<(string,string)> Sorters { get; private set; }

        internal bool HasSorters
        {
            get { return Sorters.Count > 0; }
        }

        public ORMLikeExpression()
        {
            Sorters = new List<(string, string)>(5);
        }

        public void Add(string sql, string column)
        {
            Sorters.Add((sql, column));
        }

        public void AddRange((string,string)[] likeExpressions)
        {
            Sorters.AddRange(likeExpressions);
        }
    }
}