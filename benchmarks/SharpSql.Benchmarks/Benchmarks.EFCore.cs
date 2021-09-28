using BenchmarkDotNet.Attributes;

namespace SharpSql.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class EFCoreBenchmarks
    {
        [GlobalSetup]
        public void Init()
        {

        }
    }
}