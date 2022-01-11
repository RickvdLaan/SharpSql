using System;

namespace SharpSql.Attributes
{
    public sealed class SharpSqlColumnAttribute : Attribute
    {
        public string ColumnName { get; private set; }

        public SharpSqlColumnAttribute(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException();

            ColumnName = columnName;
        }
    }
}