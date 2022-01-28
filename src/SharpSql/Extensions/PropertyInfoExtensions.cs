using SharpSql.Attributes;
using System.Reflection;

namespace SharpSql;

public static class PropertyInfoExtensions
{
    public static string Name(this PropertyInfo propertyInfo)
    {
        if (typeof(SharpSqlEntity).IsAssignableFrom(propertyInfo.DeclaringType))
        {
            var customAttribute = propertyInfo.GetCustomAttribute<SharpSqlColumnAttribute>(true);
            if (customAttribute != null)
            {
                return customAttribute.ColumnName;
            }
        }
        return propertyInfo.Name;
    }
}