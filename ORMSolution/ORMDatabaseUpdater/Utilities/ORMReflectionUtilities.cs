using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ORMDatabaseUpdater
{
    internal abstract class ORMReflectionUtilities
    {
        public static List<Type> GetAllEntities()
        {
#if DEBUG
            string path = $"{Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName)}\\ORMFakeDAL\\bin\\Debug\\netstandard2.1";
#else
            string path = Directory.GetCurrentDirectory();
#endif

            var entities = new List<Type>();

            foreach (string file in Directory.GetFiles(path, "*.dll"))
            {
                var assembly = Assembly.LoadFile(file);

                entities.AddRange(assembly.GetTypes().Where(entityType => entityType.GetCustomAttributes(typeof(ORMTableAttribute)).Any()).ToList());
            }

            return entities;
        }
    }
}
