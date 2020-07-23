using System;

namespace ORM.Attributes
{
    public sealed class ORMPrimaryKeyAttribute : Attribute
    {
        internal string Name { get; set; }

        public ORMPrimaryKeyAttribute() { }
    }
}
