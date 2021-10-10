using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    public class Organisation : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; internal set; } = -1;

        public string Name { get; set; }

        public Organisation() { }

        public Organisation(int fetchByUserId, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByUserId);
        }
    }
}
