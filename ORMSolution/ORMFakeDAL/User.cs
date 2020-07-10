using ORM;

namespace ORMFakeDAL
{
    public class User : ORMEntity
    {
        public int Id { get; private set; } = -1;

        public string Username { get; set; }

        public string Password { get; set; }

        public User() { }

        public User(int fetchByUserId) : base(nameof(Id))
        {
            base.FetchEntityById<Users, User>(fetchByUserId);
        }
    }
}
