using ORM;

namespace ORMConsole
{
    public class User : ORMEntity
    {
        public static class Fields
        {
            public static string Id = nameof(Id);
            public static string Username = nameof(Username);
            public static string Password = nameof(Password);
        }

        public int Id { get; private set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public User()
        {

        }
    }
}
