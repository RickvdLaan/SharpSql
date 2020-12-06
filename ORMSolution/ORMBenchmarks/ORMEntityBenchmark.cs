using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
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
        public User GetUserByIdDefault()
        {
            return new User(UserId);
        }

        [Benchmark(Baseline = false)]
        public User GetUserByIdManual()
        {
            var user = new User();

            using (SqlConnection sqlConnection = new SqlConnection("Server=localhost; Database=ORM; Trusted_Connection=True; MultipleActiveResultSets=true"))
            {
                using var command = new SqlCommand($"SELECT TOP (1) * FROM USERS AS U LEFT JOIN ORGANISATIONS AS O ON O.Id = U.Organisation WHERE U.ID = { UserId }", sqlConnection);
                command.Connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.VisibleFieldCount; i++)
                    {
                        if (i == 6)
                            break;

                        var propertyName = reader.GetName(i);
                        var propertyValue = reader.GetValue(i);

                        if (propertyName == nameof(user.Organisation))
                        {
                            user.Organisation = new Organisation();
                            user.Organisation[reader.GetName(6)] = reader.GetValue(6);
                            user.Organisation[reader.GetName(7)] = reader.GetValue(7);
                        }
                        else
                        {
                            user[propertyName] = propertyValue;
                        }
                    }
                }
            }

            return user;
        }
    }
}
