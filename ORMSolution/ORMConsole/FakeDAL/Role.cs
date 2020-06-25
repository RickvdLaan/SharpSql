using ORM;

namespace ORMConsole
{
    public class Role : ORMEntity
    {
        public class Fields
        {
            public static ORMEntityField Id { get { return new ORMEntityField(nameof(Id)); } }

            public static ORMEntityField Name { get { return new ORMEntityField(nameof(Name)); } }
        }

        public int Id { get; private set; } = -1;

        public string Name { get; set; }

        public Role()
        {

        }
    }
}
