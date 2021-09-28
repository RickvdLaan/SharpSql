using SharpSql.Attributes;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SharpSql.Benchmarks")]

namespace SharpSql.Northwind
{
    public class Order : ORMEntity
    {
        [ORMPrimaryKey]
        public int OrderId { get; internal set; } = 0;

        [ORMStringLength(5)]
        public string CustomerID { get; set; }
        public int? EmployeeID { get; set; }

        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int? ShipVia { get; set; }
        public decimal? Freight { get; set; }

        [ORMStringLength(40)]
        public string ShipName { get; set; }
        [ORMStringLength(60)]
        public string ShipAddress { get; set; }
        [ORMStringLength(15)]
        public string ShipCity { get; set; }
        [ORMStringLength(15)]
        public string ShipRegion { get; set; }
        [ORMStringLength(10)]
        public string ShipPostalCode { get; set; }
        [ORMStringLength(15)]
        public string ShipCountry { get; set; }

        public Order() { }

        public Order(int fetchByOrderId, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByOrderId);
        }
    }
}
