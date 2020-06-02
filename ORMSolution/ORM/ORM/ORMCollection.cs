using System;
using System.Collections;

namespace ORM
{
    public class ORMCollection : ORMObject, IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public virtual ORMCollection Fetch()
        {
            using (SQLBuilder sqlBuilder = new SQLBuilder(this))
            {
                return sqlBuilder.ExecuteCollectionQuery();
            }
        }
    }
}
