using SharpSql.Attributes;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpSql.Benchmarks")]

namespace SharpSql.NUnit
{
    public class User : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; internal set; } = -1;

        [ORMUniqueConstraint]
        [ORMStringLength(200)]
        public string Username { get; set; }

        public string Password { get; set; }

        [ORMForeignKey(typeof(Organisation))]
        public Organisation Organisation { get; set; }

        public DateTime? DateCreated { get; internal set; }

        public DateTime? DateLastModified { get; internal set; }

        [ORMManyToMany]
        public Roles Roles { get; set; }

        public User() { }

        public User(int fetchByUserId, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByUserId);
        }

        public User(int fetchByUserId, Expression<Func<User, object>> joins, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey<User>(fetchByUserId, joins);
        }

        public User FetchByUsername(string username)
        {
            base.FetchUsingUC(nameof(Username), username);

            if (IsNew)
                return null;

            return this;
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
