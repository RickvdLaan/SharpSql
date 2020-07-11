using ORM;
using ORM.Attributes;

namespace ORMFakeDAL
{
    [ORMTable(typeof(Organisations), typeof(Organisation))]
    public class Organisations : ORMCollection<Organisation>
    {
        public Organisations() { }
    }
}
