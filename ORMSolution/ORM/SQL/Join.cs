using ORM.Attributes;
using System.Reflection;

namespace ORM
{
    enum JoinType
    {
        Left,
        Right,
        Inner
    }

    internal class Join
    {
        internal JoinType type;

        internal ORMTableAttribute lTableAttr;
        internal PropertyInfo lProperty;

        internal ORMTableAttribute rTableAttr;
        internal PropertyInfo rProperty;
    }
}