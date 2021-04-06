using System.Collections.Generic;
using System.Linq;

namespace ORM
{
    internal struct DirtyTracker
    {
        private Dictionary<string, bool> DirtyList { get; set; }

        public bool Any { get { return DirtyList.Any(x => x.Value); } }

        public int Count { get { return DirtyList.Where(x => x.Value).Count(); } }

        public DirtyTracker(int capacity)
        {
            DirtyList = new Dictionary<string, bool>(capacity);
        }

        public bool IsDirty(string columnName)
        {
            return DirtyList[columnName];
        }

        public bool AnyDirtyRelations(ORMEntity entity)
        {
            return DirtyList.Any(x => entity.Relations.Any(e => e.GetType().Name != x.Key));
        }

        public void Update(string columnName, bool isDirty)
        {
            DirtyList[columnName] = isDirty;
        }
    }
}
