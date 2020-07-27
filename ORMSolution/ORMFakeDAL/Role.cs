using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    public class Role : ORMEntity
    {
        [ORMPrimaryKey]
        public int Id { get; private set; } = -1;

        [ORMColumn(RoleConstants.ColumnName)]
        public string Description { get; set; }

        public Role(int id)
        {
            base.FetchEntityById<Roles, Role>(id);
        }
    }
}
