using NorthwindDAL.Entities;
using ORM;
using ORM.Attributes;

namespace NorthwindDAL.Collections
{
    [ORMTable(typeof(Customers), typeof(Customer))]
    public class Customers : ORMCollection<Customer>
    {
        public Customers() { }
    }
}
