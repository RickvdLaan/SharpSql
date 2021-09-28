using SharpSql.Attributes;

namespace SharpSql.NUnit
{
    [ORMTable(typeof(Tokens), typeof(Token))]
    public class Tokens : ORMCollection<User>
    {
        public Tokens() { }
    }
}
