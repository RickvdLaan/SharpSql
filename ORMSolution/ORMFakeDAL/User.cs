using ORM;
using ORM.Attributes;
using System;

namespace ORMFakeDAL
{
    public class User : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; private set; } = -1;

        public string Username { get; set; }

        public string Password { get; set; }

        public Organisation Organisation { get; set; }

        public DateTime? DateCreated { get; private set; }

        public DateTime? DateLastModified { get; private set; }

        public Roles Roles { get; set; }

        public User() { }

        public User(int fetchByUserId, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByUserId);
        }

        public override void Save()
        {
            if (IsDirty)
            {
                DateLastModified = DateTime.Now;

                if (IsNew)
                {
                    DateCreated = DateLastModified;
                }
            }

            base.Save();
        }
    }
}
