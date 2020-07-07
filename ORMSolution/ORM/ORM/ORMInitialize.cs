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
            ORMUtilities.EntityTypes = new Dictionary<Type, Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attributes = type.GetCustomAttributes(typeof(ORMTableAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var tableAttribute = (attributes.First() as ORMTableAttribute);
                        ORMUtilities.EntityTypes.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        ORMUtilities.EntityTypes.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                    }
                }
            }
        }
    }
}
