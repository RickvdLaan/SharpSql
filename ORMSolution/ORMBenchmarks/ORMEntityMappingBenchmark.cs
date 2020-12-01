using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ORM;
using ORMFakeDAL;
using System;

namespace ORMBenchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ORMEntityMappingBenchmark
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
        public User DefaultMapping()
        {
            return new User(UserId);
        }

        [Benchmark(Baseline = false)]
        public User ManualMapping()
        {
            // ToDo: Manual query for both cases and set the values through the datatable for accurate results.

            var organisation = new Organisation()
            {
                Id = 108,
                Name = "IkBenNieuw"
            };

            var user = new User
            {
                Id = 1,
                Username = "username",
                Password = "password",
                Organisation = organisation,
                DateCreated = null,
                DateLastModified = Convert.ToDateTime("2020-09-16 17:20:48.087"),
                Roles = null
            };

            return user;
        }
    }
}
