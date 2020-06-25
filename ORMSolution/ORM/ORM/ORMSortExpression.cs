using ORM.Interfaces;
using System.Collections.Generic;

namespace ORM
{
    public sealed class ORMSortExpression
    {
        internal List<IORMSortClause> Sorters { get; private set; }

        internal bool HasSorters
        {
            get { return Sorters.Count > 0; }
        }

        public ORMSortExpression()
        {
            Sorters = new List<IORMSortClause>(5);
        }

        public void Add(IORMSortClause sortClause)
        {
            Sorters.Add(sortClause);
        }

        public void AddRange(IORMSortClause[] sortClauses)
        {
            Sorters.AddRange(sortClauses);
        }
    }
}