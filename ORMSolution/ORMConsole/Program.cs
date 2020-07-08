using Microsoft.Extensions.Configuration;
using ORM;
using ORMFakeDAL;
using System;

namespace ORMConsole
{
    class Program
    {
        static void Main()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            new ORMInitialize(configuration);

            Users users = new Users();
            users.OrderBy(User.Fields.Id & ORMSortType.Ascending);
            users.Fetch();
            ShowOutput(users);

            users = new Users();
            users.OrderBy(User.Fields.Username & ORMSortType.Descending);
            users.OrderBy(User.Fields.Password.Ascending());
            users.Fetch(1);
            ShowOutput(users);

            users = new Users();
            users.Select(User.Fields.Username, User.Fields.Password);
            users.Where(x => x.Id.ToString().StartsWith("1") || x.Password.Contains("qwerty") || x.Password.StartsWith("welkom"));
            users.OrderBy(User.Fields.Username.Descending(), User.Fields.Password.Ascending());
            users.Fetch();
            ShowOutput(users);

            users = new Users();
            users.Where(x => (x.Id == 1 || (x.Id == 5) || x.Id <= 3 || x.Id >= 5) || (x.Id < 2 || x.Id > 7));
            users.Fetch(1);
            ShowOutput(users);

            users = new Users();
            users.Where(x => (((x.Id == 2 || x.Id == 3) || (x.Id == 3 && x.Id == 4)) || x.Id == 5));
            users.Fetch();
            ShowOutput(users);

            users = ORMUtilities.ExecuteDirectQuery<Users, User>("SELECT TOP 10 * FROM USERS WHERE ((ID = @PARAM1 OR ID = @PARAM1) OR (ID = @PARAM2)) ORDER BY ID ASC;", 1, 2);
            ShowOutput(users);

            Console.Read();
        }

        private static void ShowOutput(Users users)
        {
            foreach (User user in users)
            {
                Console.WriteLine($"[{ nameof(user.Id) }] { user.Id }");
                Console.WriteLine($"[{ nameof(user.Username) }] { user.Username }");
                Console.WriteLine($"[{ nameof(user.Password) }] { user.Password }");
                Console.WriteLine("-------------------");
            }

            Console.WriteLine($"Executed query: { users.ExecutedQuery }");
            Console.WriteLine("-------------------");
        }
    }
}
