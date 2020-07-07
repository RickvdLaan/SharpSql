using ORM;

namespace ORMFakeDAL
{
    public class User : ORMEntity
    {
        public class Fields
        {
            public static ORMEntityField Id { get { return new ORMEntityField(nameof(Id)); } }

            public static ORMEntityField Username { get { return new ORMEntityField(nameof(Username)); } }

            public static ORMEntityField Password { get { return new ORMEntityField(nameof(Password)); } }
        }

        public int Id { get; private set; } = -1;

        public string Username { get; set; }

        public string Password { get; set; }

        public User()
        {

        }
    }
}
