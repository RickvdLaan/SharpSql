using System;

namespace ORM.Attributes
{
    public sealed class ORMPrimaryKeyAttribute : Attribute
    {
        internal string Name { get; set; }

        internal bool IsAutoIncrement { get; set; }

        public ORMPrimaryKeyAttribute(bool isAutoIncrement = true) 
        {
            IsAutoIncrement = isAutoIncrement;
        }
    }
}
