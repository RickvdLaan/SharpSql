using ORM;

namespace ORMConsole
{
    public class User : ORMEntity
    {
        public int Id { get; private set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public User()
        {

        }
    }
}
