using ORM.Attributes;
using System;
using System.Reflection;

namespace ORM
{
    internal static class Extensions
    {
        internal static string Description(this Enum source)
        {
            FieldInfo fieldInfo = source.GetType().GetField(source.ToString());

            ORMSQLAttribute[] attributes = (ORMSQLAttribute[])fieldInfo.GetCustomAttributes(typeof(ORMSQLAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].SQL;
            }

            throw new NotImplementedException();
        }
    }
}