using SharpSql.Attributes;

namespace SharpSql.Northwind
{
    [ORMTable(typeof(Customers), typeof(Customer))]
    public class Customers : ORMCollection<Customer>
    {
        public Customers() { }
    }
}
