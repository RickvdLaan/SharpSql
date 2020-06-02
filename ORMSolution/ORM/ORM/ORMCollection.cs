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
            SQLBuilder sqlBuilder = new SQLBuilder(this);
            sqlBuilder.OpenConnection();

            // Do stuff

            sqlBuilder.CloseConnection();

            return null;
        }
    }
}
