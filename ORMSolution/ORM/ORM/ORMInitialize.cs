using Microsoft.Extensions.Configuration;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMNUnit")]
namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMUtilities Utilities { get; set; }

        public ORMInitialize(IConfiguration configuration) : this()
        {
            Utilities = new ORMUtilities(configuration);
        }

        internal ORMInitialize()
        {
            Dictionary<Type, Type> entityTypes = new Dictionary<Type, Type>();
            var a = AppDomain.CurrentDomain.GetAssemblies();
            var b = a.Select(x => x.FullName).OrderBy(x => x).ToList();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(ORMTableAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var tableAttribute = (attributes.First() as ORMTableAttribute);
                        entityTypes.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        entityTypes.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                    }
                }
            }

            ORMUtilities.EntityTypes = entityTypes;
        }
    }
}
