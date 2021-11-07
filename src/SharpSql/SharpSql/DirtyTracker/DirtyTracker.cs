using System.Collections.Generic;
using System.Linq;

namespace SharpSql
{
    internal struct DirtyTracker
    {
        private Dictionary<string, bool> DirtyList { get; set; }

        public bool Any { get { return DirtyList.Any(x => x.Value); } }

        public int Count { get { return DirtyList.Where(x => x.Value).Count(); } }

        public DirtyTracker(List<string> fields)
        {
            DirtyList = new Dictionary<string, bool>(fields.Count);

            for (int i = 0; i < fields.Count; i++)
            {
                Update(fields[i], false);
            }
        }

        public bool IsDirty(string columnName)
        {
            if (DirtyList.ContainsKey(columnName))
            {
                return DirtyList[columnName];
            }

            throw new KeyNotFoundException(columnName);
        }

        public bool AnyDirtyRelations(SharpSqlEntity entity)
        {
            return DirtyList.Any(x => entity.Relations.Any(e => e.GetType().Name != x.Key));
        }

        public void Update(string columnName, bool isDirty)
        {
            DirtyList[columnName] = isDirty;
        }

        internal void Reset()
        {
            if (Any)
            {
                foreach (var key in DirtyList.Keys)
                {
                    Update(key, false);
                }
            }
        }
    }
}
