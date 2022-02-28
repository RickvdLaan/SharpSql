using Microsoft.Extensions.Configuration;
using SharpSql;
using SharpSql.NUnit;

IConfiguration configuration = new ConfigurationBuilder()
                  .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                  .Build();

var _ = new SharpSqlInitializer(configuration, true, true);











var user = new User(1, x => x.Roles2.Left());

Console.WriteLine(user);

var users = (new Users()
      .Join(x => new object[] { x.Roles2.Left() })
      .Fetch() as Users);

Console.WriteLine(users);