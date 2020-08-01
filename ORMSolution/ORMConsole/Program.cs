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

            var user = new Users();
            user.Join(x => new object[]{ x.Roles.Right(), x.Organisation.Right() });
            user.Where(x => x.Id == 1);

            user.Fetch();
            ShowOutput(user);
            Console.Read();
        }

        private static void ShowOutput(Users users)
        {
            foreach (User user in users)
            {
                ShowOutput(user, false);
            }

            Console.WriteLine($"Executed query: { users.ExecutedQuery }");
            Console.WriteLine("-------------------");
        }
        
        private static void ShowOutput(User user, bool displayExecutedQuery = true)
        {
            Console.WriteLine($"[{ nameof(user.Id) }] { user.Id }");
            Console.WriteLine($"[{ nameof(user.Username) }] { user.Username }");
            Console.WriteLine($"[{ nameof(user.Password) }] { user.Password }");
            Console.WriteLine($"[{ nameof(user.Organisation) }] id: { user.Organisation?.Id }, name: { user.Organisation?.Name }");
            Console.WriteLine("-------------------");

            if (displayExecutedQuery)
            {
                Console.WriteLine($"Executed query: { user.ExecutedQuery }");
                Console.WriteLine("-------------------");
            }
        }
    }
}
