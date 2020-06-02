using Microsoft.Extensions.Configuration;

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal Utilities Utilities { get; set; }

        public ORMInitialize(IConfiguration configuration)
        {
            Utilities = new Utilities(configuration);
        }
    }
}
