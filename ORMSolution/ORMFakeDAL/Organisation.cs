using ORM;

namespace ORMFakeDAL
{
    public class Organisation : ORMEntity
    {
        public int Id { get; private set; } = -1;

        public string Name { get; set; }

        public Organisation():base(nameof(Id)) { }

        public Organisation(int fetchByUserId) : base(nameof(Id))
        {
            base.FetchEntityById<Organisations, Organisation>(fetchByUserId);
        }
    }
}
