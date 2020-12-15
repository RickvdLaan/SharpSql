using System;

namespace ORM.Attributes
{
    public sealed class ORMPrimaryKeyAttribute : Attribute
    {
        internal string PropertyName { get; set; }

        internal string ColumnName { get; set; }

        internal bool IsAutoIncrement { get; set; }

        public ORMPrimaryKeyAttribute(bool isAutoIncrement = true) 
        {
            IsAutoIncrement = isAutoIncrement;
        }
    }
}
