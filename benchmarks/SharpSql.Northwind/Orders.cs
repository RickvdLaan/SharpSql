using SharpSql.Attributes;

namespace SharpSql.Northwind
{
    [ORMTable(typeof(Orders), typeof(Order))]
    public class Orders : ORMCollection<Order>
    {
        public Orders() { }
    }
}
