using SharpSql.Attributes;
using System;

namespace SharpSql
{
    public class Order : SharpSqlEntity
    {
        [SharpSqlPrimaryKey]
        public int OrderId { get; internal set; } = 0;

        [SharpSqlStringLength(5)]
        public string CustomerID { get; set; }
        public int? EmployeeID { get; set; }

        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int? ShipVia { get; set; }
        public decimal? Freight { get; set; }

        [SharpSqlStringLength(40)]
        public string ShipName { get; set; }
        [SharpSqlStringLength(60)]
        public string ShipAddress { get; set; }
        [SharpSqlStringLength(15)]
        public string ShipCity { get; set; }
        [SharpSqlStringLength(15)]
        public string ShipRegion { get; set; }
        [SharpSqlStringLength(10)]
        public string ShipPostalCode { get; set; }
        [SharpSqlStringLength(15)]
        public string ShipCountry { get; set; }

        public Order() { }

        public Order(int fetchByOrderId, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByOrderId);
        }
    }
}
