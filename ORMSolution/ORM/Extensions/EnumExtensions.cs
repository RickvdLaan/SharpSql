﻿using ORM.Attributes;
using System;

namespace ORM
{
    internal static class EnumExtensions
    {
        internal static string SQL(this Enum source)
        {
            var fieldInfo = source.GetType().GetField(source.ToString());

            var attributes = (ORMSQLAttribute[])fieldInfo.GetCustomAttributes(typeof(ORMSQLAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].SQL;
            }

            throw new NotImplementedException();
        }
    }
}