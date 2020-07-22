using ORM;
using ORMFakeDAL;

namespace ORMNUnit
{
    public abstract class NUnitUtilities
    {
        public static void InitializeORM()
        {
            // Hack to force load the ORMFakeDAL assembly since the ORM has no clue of its existance
            // during initialization.
            new Users();
            // ¯\_(ツ)_/¯

            new ORMInitialize();
        }
    }
}
