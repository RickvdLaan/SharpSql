using SharpSql.Attributes;

namespace SharpSql;

[SharpSqlTable(typeof(Orders), typeof(Order))]
public class Orders : SharpSqlCollection<Order>
{
    public Orders() { }
}