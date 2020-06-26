using Microsoft.Extensions.Configuration;

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMUtilities Utilities { get; set; }

        public ORMInitialize(IConfiguration configuration)
        {
            Utilities = new ORMUtilities(configuration);
        }
    }
}
