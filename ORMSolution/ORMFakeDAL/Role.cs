using ORM;

namespace ORMFakeDAL
{
    public class Role : ORMEntity
    {
        public int Id { get; private set; } = -1;

        public string Name { get; set; }

        public Role() : base(nameof(Id)) { }

        public Role(int id) : base(nameof(Id))
        {
            base.FetchEntityById<Roles, Role>(id);
        }
    }
}
