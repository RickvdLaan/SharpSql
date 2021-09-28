using System;

namespace SharpSql.Attributes
{
    public sealed class ORMStringLengthAttribute : Attribute
    {
        public int Length { get; private set; }

        public ORMStringLengthAttribute(int length)
        {
            Length = length;
        }
    }
}