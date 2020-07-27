using System;

namespace ORM
{
    public static class ORMEntityExtensions
    {
        public static T Ascending<T>(this T _) => default;

        public static T Descending<T>(this T _) => default;

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

        public static string ToSqlString(this DateTime @this)
        {
            return @this.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public static string SqlValue(this ORMEntity entity, string columnName, string addon)
        {
            object value;

            switch (entity.GetType().GetProperty(columnName, entity.PublicFlags).PropertyType)
            {
                case Type dateTime when dateTime == typeof(DateTime?):
                    if (((DateTime?)entity[columnName]).HasValue)
                        value = ((DateTime?)entity[columnName]).Value.ToSqlString();
                    else
                        return $"NULL{addon}";
                    break;
                case Type dateTime when dateTime == typeof(DateTime):
                    value = ((DateTime)entity[columnName]).ToSqlString();
                    break;
                default:
                    value = entity[columnName];
                    break;
            }

            if (value == null)
            {
                return $"NULL{addon}";
            }
            else
            {
                return $"'{value}'{addon}";
            }
        }
    }
}
