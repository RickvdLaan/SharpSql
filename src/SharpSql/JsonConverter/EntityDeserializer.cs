using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Reflection;

namespace SharpSql
{
    public class EntityConverter : CustomCreationConverter<ORMEntity>
    {
        public override ORMEntity Create(Type objectType)
        {
            var constructorInfo = typeof(ORMEntity).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
            var entity = constructorInfo.Invoke(new object[] { ObjectState.ExternalRecord }) as ORMEntity;

            return entity;
        }
    }
}
