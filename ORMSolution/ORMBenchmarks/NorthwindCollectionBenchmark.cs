using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using NorthwindDAL.Collections;
using ORM;

namespace ORMBenchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class NorthwindCollectionBenchmark
    {

        [GlobalSetup]
        public void Init()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _ = new ORMInitialize(configuration, loadAllReferencedAssemblies: true);
        }

        [Benchmark(Baseline = true)]
        public Customers GetAllCustomersORM()
        {
            return new Customers().Fetch() as Customers;
        }
    }
}
