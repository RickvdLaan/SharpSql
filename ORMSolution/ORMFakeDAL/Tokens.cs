using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(Tokens), typeof(Token))]
    public class Tokens : ORMCollection<User>
    {
        public Tokens() { }
    }
}
