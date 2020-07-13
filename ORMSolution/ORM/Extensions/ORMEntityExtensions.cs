namespace ORM
{
    public static class ORMEntityExtensions
    {
        public static T Ascending<T>(this T _) => default;

        public static T Descending<T>(this T _) => default;

        public static ORMEntity Inner(this ORMEntity _) => default;

        public static ORMEntity Left(this ORMEntity _) => default;

        public static ORMEntity Right(this ORMEntity _) => default;

        public static ORMEntity Full(this ORMEntity _) => default;

        public static bool Contains<T>(this T @this, string value)
        {
            return @this.Contains(value);
        }

        public static bool StartsWith<T>(this T @this, string value)
        {
            return @this.StartsWith(value);
        }

        public static bool EndsWith<T>(this T @this, string value)
        {
            return @this.EndsWith(value);
        }
    }
}
