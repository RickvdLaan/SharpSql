using System;

namespace SharpSql.Attributes
{
    public sealed class SharpSqlStringLengthAttribute : Attribute
    {
        public int Length { get; private set; }

        public SharpSqlStringLengthAttribute(int length)
        {
            Length = length;
        }
    }
}