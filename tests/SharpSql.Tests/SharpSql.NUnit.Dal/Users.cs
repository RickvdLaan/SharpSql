using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [ORMTable(typeof(Users), typeof(User))]
    public class Users : ORMCollection<User>
    {
        public Users() { }
    }
}
