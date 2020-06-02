using Microsoft.Extensions.Configuration;

namespace ORM
{
    internal class Utilities
    {
        public Utilities(IConfiguration configuration)
        {
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
        }

        internal static string ConnectionString { get; private set; }
    }
}