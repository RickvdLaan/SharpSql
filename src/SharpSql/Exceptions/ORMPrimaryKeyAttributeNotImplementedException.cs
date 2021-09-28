﻿using System;

namespace SharpSql.Exceptions
{
    [Serializable]
    public class ORMPrimaryKeyAttributeNotImplementedException : Exception
    {
        public ORMPrimaryKeyAttributeNotImplementedException(Type type)
            : base($"ORMPrimaryKeyAttribute is not implemented in entity { type.Name }.")
        { }
    }
}
