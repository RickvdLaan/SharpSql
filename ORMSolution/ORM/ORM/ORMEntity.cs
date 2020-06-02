using ORM.Interfaces;
using System;

namespace ORM
{
    public class ORMEntity : ORMObject, IORMEntity
    {
        public virtual void Save()
        {
            throw new NotImplementedException();
        }

        public virtual void Delete()
        {
            throw new NotImplementedException();
        }
    }
}