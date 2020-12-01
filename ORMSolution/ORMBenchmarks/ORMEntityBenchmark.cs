using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using ORM;
using ORMFakeDAL;

namespace ORMBenchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ORMEntityBenchmark
    {
        private const int UserId = 1;

        [GlobalSetup]
        public void Init()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _ = new ORMInitialize(configuration, loadAllReferencedAssemblies: true);
        }

        [Benchmark(Baseline = true)]
        public User GetUserById()
        {
            return new User(UserId);
        }

        [Benchmark(Baseline = false)]
        public Users GetAllUsers()
        {
            var users = new Users();
            users.Fetch();
            return users;
        }
    }
}
