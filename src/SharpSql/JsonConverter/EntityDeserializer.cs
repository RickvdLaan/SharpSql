﻿using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Reflection;

namespace SharpSql
{
    public class EntityDeserializer : CustomCreationConverter<ORMEntity>
    {
        public override ORMEntity Create(Type objectType)
        {
            var externalEntity = Activator.CreateInstance(objectType) as ORMEntity;

            var constructorInfo = typeof(ORMEntity).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
            var entity = constructorInfo.Invoke(new object[] { objectType, ObjectState.ExternalRecord }) as ORMEntity;

            entity.CloneToChild(externalEntity);

            return externalEntity;
        }
    }
}
