using ORM;
using ORM.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMBenchmarks")]

namespace ORMFakeDAL
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
