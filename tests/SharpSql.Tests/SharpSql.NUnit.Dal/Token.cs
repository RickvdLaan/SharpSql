using SharpSql.Attributes;
using System;

namespace SharpSql.NUnit
{
    public class Token : SharpSqlEntity
    {
        [SharpSqlPrimaryKey(false)]
        public Guid UserId { get; internal set; } = Guid.Empty;

        [SharpSqlPrimaryKey(false), SharpSqlUniqueConstraint]
        public Guid TokenId { get; internal set; } = Guid.Empty;

        public DateTime Expired { get; internal set; } = DateTime.MinValue;

        internal Token() { }

        public Token(Guid userId, DateTime expired)
        {
            UserId = userId;
            TokenId = Guid.NewGuid();
            Expired = expired;
        }
    }
}
