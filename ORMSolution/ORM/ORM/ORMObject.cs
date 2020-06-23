using ORM.SQL;
using System;
using System.Data;

namespace ORM
{
    public class ORMObject : object
    {
        public bool IsInTransaction()
        {
            throw new NotImplementedException();
        }

        public void TransactionBegin()
        {
            throw new NotImplementedException();
        }

        public void TransactionCommit()
        {
            throw new NotImplementedException();
        }

        public void TransactionRollback()
        {
            throw new NotImplementedException();
        }

        public DataTable DirectQuery(string query, params object[] parameters)
        {
            using (SQLConnection connection = new SQLConnection())
            {
                return connection.ExecuteDirectQuery(query, parameters);
            }
        }
    }
}
