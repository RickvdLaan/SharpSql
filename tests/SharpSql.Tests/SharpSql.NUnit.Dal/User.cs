using SharpSql.Attributes;
using System;
using System.Linq.Expressions;

namespace SharpSql.NUnit
{
    public class User : SharpSqlEntity
    {
        [SharpSqlPrimaryKey]
        public int Id { get; internal set; } = -1;

        [SharpSqlUniqueConstraint]
        [SharpSqlStringLength(200)]
        public string Username { get; set; }

        public string Password { get; set; }

        [SharpSqlForeignKey(typeof(Organisation))]
        public Organisation Organisation { get; set; }

        public DateTime? DateCreated { get; internal set; }

        public DateTime? DateLastModified { get; internal set; }

        [SharpSqlManyToMany]
        public Roles Roles { get; set; }

        public User() { }

        public User(bool disableChangeTracking) : base(disableChangeTracking) { }

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
