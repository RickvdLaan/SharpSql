using System;

namespace ORM.Attributes
{
    public sealed class ORMPrimaryKeyAttribute : Attribute
    {
        internal string Name { get; set; }

        internal bool AutoIncrement { get; set; }

        public ORMPrimaryKeyAttribute(bool autoIncrement = false) 
        {
            AutoIncrement = autoIncrement;
        }
    }
}
