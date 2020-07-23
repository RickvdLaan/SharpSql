using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class RoleEntity : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; private set; } = -1;

        public string Role { get; set; }

        public RoleEntity() { }

        public RoleEntity(int id)
        {
            base.FetchEntityById<Roles, RoleEntity>(id);
        }
    }
}
