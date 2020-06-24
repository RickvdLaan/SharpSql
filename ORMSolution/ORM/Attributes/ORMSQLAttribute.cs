using System;

namespace ORM.Attributes
{
    internal sealed class ORMSQLAttribute : Attribute
    {
        public string SQL { get; set; }

        public ORMSQLAttribute(string sql)
        {
            SQL = sql;
        }
    }
}
