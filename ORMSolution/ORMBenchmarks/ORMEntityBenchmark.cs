using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ORM;
using ORMFakeDAL;
using System.Linq;

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
        public User GetUserByIdDefault()
        {
            return new User(UserId, x => x.Organisation.Left());
        }

        [Benchmark(Baseline = false)]
        public User GetUserByIdDefaultLeftJoin()
        {
            return new User(UserId, x => x.Organisation);
        }


        [Benchmark(Baseline = false)]
        public User GetUserByIdDefaultNoChangeTracking()
        {
            return new User(UserId, x => x.Organisation.Left(), true);
        }

        [Benchmark(Baseline = false)]
        public ORMFakeEF.User GetUserByIdDefaultEF()
        {
            using (var db = new ORMDBContext())
            {
                return db.Users.Include(x => x.Organisation).Where(user => user.Id == 1).ToList().FirstOrDefault();
            }
        }

        [Benchmark(Baseline = false)]
        public User GetUserByIdManual()
        {
            //var user = new User();

            using (SqlConnection sqlConnection = new SqlConnection("Server=localhost; Database=ORM; Trusted_Connection=True; MultipleActiveResultSets=true"))
            {
                using var command = new SqlCommand($"SELECT TOP (1) * FROM USERS AS U LEFT JOIN ORGANISATIONS AS O ON O.Id = U.Organisation WHERE U.ID = { UserId }", sqlConnection);
                command.Connection.Open();

                FetchAndFillObject(command);//, user);
            }

            return null;
        }

        // Make into nice util function.

        private void FetchAndFillObject(SqlCommand command)//, ORMEntity entity)
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                {
                    //if (i == entity.TableScheme.Count) // 6? check if 6 for user and if this is a good generic option?
                        // what if certain fields are not available? does it break?
                    //    break;

                    var propertyName = reader.GetName(i);
                    var propertyValue = reader.GetValue(i);

                    //if (entity[propertyName].GetType().IsAssignableFrom(typeof(ORMEntity)))
                    //{
                        //entity[propertyName] = Activator.CreateInstance(entity[propertyName].GetType());
                        //(entity[propertyName] as ORMEntity)[reader.GetName(6)] = reader.GetValue(6);
                        //(entity[propertyName] as ORMEntity)[reader.GetName(7)] = reader.GetValue(7);
                    //}
                    //else
                    //{
                        //entity[propertyName] = propertyValue;
                    //}
                }
            }
        }
    }
}
