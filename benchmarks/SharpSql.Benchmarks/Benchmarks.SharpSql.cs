using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using SharpSql.Northwind;
using System.ComponentModel;
using System.Linq;

namespace SharpSql.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [Description(nameof(SharpSqlBenchmarks))]
    public class SharpSqlBenchmarks : BaseBenchmark
    {
        [GlobalSetup]
        public void Init()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _ = new SharpSqlInitializer(configuration, loadAllReferencedAssemblies: true);

            BaseSetup();
        }

        [Benchmark(Description = nameof(GetAllOrders))]
        public Orders GetAllOrders()
        {
            return new Orders().Fetch() as Orders;
        }

        [Benchmark(Description = nameof(GetAllOrderIndividual))]
        public Order GetAllOrderIndividual()
        {
            Step();
            return new Order(i);
        }

        [Benchmark(Description = nameof(NoTracking))]
        public Order NoTracking()
        {
            Step();
            return new Order(i, true);
        }

        [Benchmark(Description = nameof(SqlQuery))]
        public Order SqlQuery()
        {
            Step();
            return DatabaseUtilities.ExecuteDirectQuery<Orders, Order>("select * from Orders where OrderID = @PARAM1", false, 1).First();
        }
    }
}
