using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace SharpSql.Benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
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

        [Benchmark(Description = nameof(GetOrderById))]
        public Order GetOrderById()
        {
            return new Order(10248);
        }

        [Benchmark(Description = nameof(GetAllOrders))]
        public Orders GetAllOrders()
        {
            return new Orders().Fetch() as Orders;
        }

        [Benchmark(Description = nameof(GetAllOrdersIndividually))]
        public Order GetAllOrdersIndividually()
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

        [Benchmark(Description = nameof(SqlQueryMapped))]
        public Order SqlQueryMapped()
        {
            Step();
            return DatabaseUtilities.ExecuteDirectQuery<Orders, Order>("select * from Orders where OrderID = @PARAM1;", false, i).First();
        }

        [Benchmark(Description = nameof(SqlQueryDefault))]
        public DataTable SqlQueryDefault()
        {
            Step();
            return DatabaseUtilities.ExecuteDirectQuery("select * from Orders where OrderID = @PARAM1;", i);
        }
    }
}
