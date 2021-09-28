using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using SharpSql.Northwind;

namespace SharpSql.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class SharpSqlBenchmarks
    {
        [GlobalSetup]
        public void Init()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _ = new SharpSqlInitializer(configuration, loadAllReferencedAssemblies: true);
        }

        [Benchmark(Baseline = true)]
        public Customers GetAllCustomersORM()
        {
            return new Customers().Fetch() as Customers;
        }
    }
}
