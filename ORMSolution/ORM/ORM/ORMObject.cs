using System;

namespace ORM
{
    public class ORMObject : object
    {
        public string GetQuery()
        {
            throw new NotImplementedException();
        }

        public string GetStacktrace()
        {
            throw new NotImplementedException();
        }

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
    }
}
