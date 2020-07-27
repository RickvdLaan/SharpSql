using System;

namespace ORM.Attributes
{
    public sealed class ORMColumnAttribute : Attribute
    {
        public string ColumnName { get; private set; }

        public ORMColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}