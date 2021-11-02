using System;

namespace SharpSql.Attributes
{
    public sealed class SharpSqlPrimaryKeyAttribute : Attribute
    {
        internal string PropertyName { get; set; }

        internal string ColumnName { get; set; }

        internal bool IsAutoIncrement { get; set; }

        public SharpSqlPrimaryKeyAttribute(bool isAutoIncrement = true) 
        {
            IsAutoIncrement = isAutoIncrement;
        }
    }
}
