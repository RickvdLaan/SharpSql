//using BenchmarkDotNet.Attributes;
//using Microsoft.Data.SqlClient;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using ORM;
//using ORMFakeDAL;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;

//namespace ORMBenchmarks
//{
//    [MemoryDiagnoser]
//    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
//    [RankColumn]
//    public class ORMCollectionBenchmark
//    {
  
//        [GlobalSetup]
//        public void Init()
//        {
//            IConfiguration configuration = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json")
//                .Build();

//            _ = new ORMInitialize(configuration, loadAllReferencedAssemblies: true);
//        }

//        [Benchmark(Baseline = true)]
//        public void GetAllUsersORM()
//        {
//            var users = new Users().Join(x => x.Organisation).Fetch() as Users;

//            foreach (var user in users)
//            {
//                if (user != null)
//                {
//                    var id = user.Id;
//                    var username = user.Username;
//                    var password = user.Password;
//                    var organisation = user.Organisation;
//                    var dateCreated = user.DateCreated;
//                }
//            }
//        }

//        [Benchmark(Baseline = false)]
//        public void GetAllUsersEFSimple()
//        {
//            using (var db = new ORMDBContext())
//            {
//                var users = db.Users.Include(x => x.Organisation).ToList();

//                foreach (var user in users)
//                {
//                    if (user != null)
//                    {
//                        var id = user.Id;
//                        var username = user.Username;
//                        var password = user.Password;
//                        var organisation = user.Organisation;
//                        var dateCreated = user.DateCreated;
//                    }
//                }
//            }
//        }

//        [Benchmark(Baseline = false)]
//        public void GetAllUsersEFComplex()
//        {
//            using (var db = new ORMDBContext())
//            {
//                var result = from user in db.Users
//                             join organisation in db.Organisations on user.Id equals organisation.Id into Details
//                             from m in Details.DefaultIfEmpty()
//                             select new
//                             {
//                                 id = user.Id,
//                                 username = user.Username,
//                                 password = user.Password,
//                                 organisation = m,
//                                 DateCreated = user.DateCreated,
//                                 DateLastModified = user.DateLastModified
//                             };

//                var users = result.ToList<object>();

//                foreach (ORMFakeEF.User user in users)
//                {
//                    if (user != null)
//                    {
//                        var id = user.Id;
//                        var username = user.Username;
//                        var password = user.Password;
//                        var organisation = user.Organisation;
//                        var dateCreated = user.DateCreated;
//                    }
//                }
//            }
//        }

//        [Benchmark(Baseline = false)]
//        public void GetAllUsersAdo()
//        {
//            using (SqlConnection sqlConnection = new SqlConnection("Server=localhost; Database=ORM; Trusted_Connection=True; MultipleActiveResultSets=true"))
//            {
//                using (var command = new SqlCommand($"SELECT * FROM USERS AS U LEFT JOIN ORGANISATIONS AS O ON U.ORGANISATION = O.ID;", sqlConnection))
//                {
//                    command.Connection.Open();

//                    using (var reader = command.ExecuteReader())
//                    {
//                        var dt = new DataTable();
//                        dt.Load(reader);

//                        foreach (DataRow user in dt.Rows)
//                        {
//                            if (user != null)
//                            {
//                                var id = user["Id"];
//                                var username = user["Username"];
//                                var password = user["Password"];
//                                var organisation = user["Organisation"];
//                                var dateCreated = user["DateCreated"];
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
