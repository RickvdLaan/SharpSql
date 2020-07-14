using System.Reflection;

namespace ORM
{
    public class ORMObject : object
    {
        internal BindingFlags PublicFlags => BindingFlags.Instance | BindingFlags.Public;

        internal BindingFlags PublicIgnoreCaseFlags => PublicFlags | BindingFlags.IgnoreCase;

        internal BindingFlags NonPublicFlags => BindingFlags.Instance | BindingFlags.NonPublic;
    }
}
