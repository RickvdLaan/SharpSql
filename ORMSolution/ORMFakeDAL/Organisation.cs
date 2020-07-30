using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class Organisation : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; private set; } = -1;

        public string Name { get; set; }

        public Organisation() { }

        public Organisation(int fetchByUserId)
        {
            base.FetchEntityByPrimaryKey(fetchByUserId);
        }
    }
}
