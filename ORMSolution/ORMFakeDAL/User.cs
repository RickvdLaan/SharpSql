using ORM;

namespace ORMFakeDAL
{
    public class User : ORMEntity
    {
        public int Id { get; private set; } = -1;

        public string Username { get; set; }

        public string Password { get; set; }

        public User()
        {

        }
    }
}
