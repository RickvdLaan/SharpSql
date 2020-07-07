using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMUtilities Utilities { get; set; }

        public ORMInitialize(IConfiguration configuration)
        {
            Utilities = new ORMUtilities(configuration);
        }

        internal ORMInitialize()
        {
            Utilities = new ORMUtilities();
        }
    }
}
