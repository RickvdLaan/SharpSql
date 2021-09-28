using SharpSql;
using SharpSql.Attributes;
using System;
using System.Linq;
using System.Reflection;

public static class ORMExtensions
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

    public static (object value, string sourceColumn) SqlValue(this ORMEntity entity, string columnName)
    {
        object value;
        // use TypeCode? seperate SqlToCSharp and CSharpToSqlthe mapper to seperate class.
        switch (entity.GetPropertyInfo(columnName).PropertyType)
        {
            case Type dateTime when dateTime == typeof(DateTime?):
                if (((DateTime?)entity[columnName]).HasValue)
                    value = ((DateTime?)entity[columnName]).Value.ToSqlString();
                else
                    return (DBNull.Value, columnName);
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
            return (DBNull.Value, columnName);
        }
        else
        {
            return (value, columnName);
        }
    }

    public static PropertyInfo GetPropertyInfo(this ORMEntity entity, string propertyName)
    {
        return entity.GetType().GetProperty(propertyName, entity.PublicIgnoreCaseFlags)
            ?? entity.GetType().GetProperties().FirstOrDefault(x => (x.GetCustomAttributes(typeof(ORMColumnAttribute), false).FirstOrDefault() as ORMColumnAttribute)?.ColumnName == propertyName);
    }

    public static bool IsForeignKeyOfType(this ORMEntity entity, string propertyName, Type type)
    {
        var propertyInfo = entity.GetPropertyInfo(propertyName);

        var fkAttribute = propertyInfo.GetCustomAttributes(typeof(ORMForeignKeyAttribute), false).FirstOrDefault() as ORMForeignKeyAttribute;

        return type == fkAttribute.Relation;
    }
}