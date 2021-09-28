using SharpSql.Attributes;
using System.Reflection;

namespace SharpSql
{
    public static class PropertyInfoExtensions
    {
        public static string Name(this PropertyInfo propertyInfo)
        {
            if (typeof(ORMEntity).IsAssignableFrom(propertyInfo.DeclaringType))
            {
                var customAttribute =  propertyInfo.GetCustomAttribute<ORMColumnAttribute>(true);
                if (customAttribute != null)
                {
                    return customAttribute.ColumnName;
                }
            }
            return propertyInfo.Name;
        }
    }
}
