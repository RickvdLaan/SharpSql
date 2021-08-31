using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ORM;
using ORMFakeDAL;
using System.Collections.Generic;
using System.Linq;

namespace ORMBenchmarks
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ORMCollectionBenchmark
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
        public Users GetAllUsers()
        {
            return new Users().Join(x => x.Organisation.Left()).Fetch() as Users;
        }

        [Benchmark(Baseline = false)]
        public List<ORMFakeEF.User> GetAllUsersEFSimple()
        {
            using (var db = new ORMDBContext())
            {
                return db.Users.Include(x => x.Organisation).ToList();
            }
        }

        [Benchmark(Baseline = false)]
        public object GetAllUsersEFComplex()
        {
            using (var db = new ORMDBContext())
            {
                var result = from user in db.Users
                             join organisation in db.Organisations on user.Id equals organisation.Id into Details
                             from m in Details.DefaultIfEmpty()
                             select new
                             {
                                 id = user.Id,
                                 username = user.Username,
                                 password = user.Password,
                                 organisation = m,
                                 DateCreated = user.DateCreated,
                                 DateLastModified = user.DateLastModified
                             };

                return result;
            }
        }
    }
}
