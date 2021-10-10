using SharpSql.Attributes;
using System;

namespace SharpSql.NUnit
{
    public class Token : ORMEntity
    {
        [ORMPrimaryKey(false)]
        public Guid UserId { get; internal set; } = Guid.Empty;

        [ORMPrimaryKey(false), ORMUniqueConstraint]
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
