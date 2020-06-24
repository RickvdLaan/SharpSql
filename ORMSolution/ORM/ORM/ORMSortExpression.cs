using System;
using System.Collections.Generic;

namespace ORM
{
    public sealed class ORMSortExpression
    {
        internal List<(string Column, ORMOrderByType OrderBy)> Sorters { get; private set; }

        internal bool HasSorters
        {
            get { return Sorters.Count > 0; }
        }

        public ORMSortExpression()
        {
            Sorters = new List<(string Column, ORMOrderByType OrderBy)>(5);
        }

        public void Add(string column, ORMOrderByType orderBy)
        {
            Sorters.Add((column, orderBy));
        }
    }
}