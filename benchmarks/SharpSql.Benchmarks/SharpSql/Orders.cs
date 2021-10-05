using SharpSql.Attributes;

namespace SharpSql
{
    [ORMTable(typeof(Orders), typeof(Order))]
    public class Orders : ORMCollection<Order>
    {
        public Orders() { }
    }
}
