using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Reflection;

namespace SharpSql
{
    public class EntityDeserializer : CustomCreationConverter<SharpSqlEntity>
    {
        public override SharpSqlEntity Create(Type objectType)
        {
            var externalEntity = Activator.CreateInstance(objectType) as SharpSqlEntity;

            var constructorInfo = typeof(SharpSqlEntity).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            var entity = constructorInfo.Invoke(new object[] { objectType }) as SharpSqlEntity;

            entity.CloneToChild(externalEntity);

            return externalEntity;
        }
    }
}
