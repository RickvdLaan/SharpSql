using ORM;
using ORM.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ORMBenchmarks")]

namespace NorthwindDAL.Entities
{
    public class Customer : ORMEntity
    {
        [ORMPrimaryKey]
        [ORMStringLength(5)]
        public string CustomerID { get; internal set; } = string.Empty;

        [ORMStringLength(40)]
        public string CompanyName { get; set; }

        [ORMStringLength(30)]
        public string ContactName { get; set; }

        [ORMStringLength(30)]
        public string ContactTitle { get; set; }

        [ORMStringLength(60)]
        public string Address { get; set; }

        [ORMStringLength(15)]
        public string City { get; set; }

        [ORMStringLength(15)]
        public string Region { get; set; }

        [ORMStringLength(10)]
        public string PostalCode { get; set; }

        [ORMStringLength(15)]
        public string Country { get; set; }

        [ORMStringLength(24)]
        public string Phone { get; set; }

        [ORMStringLength(24)]
        public string Fax { get; set; }

        public Customer() { }

        public Customer(int fetchByCustomerID, bool disableChangeTracking = default) : base(disableChangeTracking)
        {
            base.FetchEntityByPrimaryKey(fetchByCustomerID);
        }
    }
}
