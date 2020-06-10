using Microsoft.Extensions.Configuration;
using ORM;
using System;

namespace ORMConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            new ORMInitialize(configuration);

            Users users = new Users();
            users.Fetch(5);

            foreach (User user in users)
            {
                Console.WriteLine($"[{ nameof(user.Id) }] { user.Id }");
                Console.WriteLine($"[{ nameof(user.Username) }] { user.Username }");
                Console.WriteLine($"[{ nameof(user.Password) }] { user.Password }");
                Console.WriteLine("-------------------");
            }

            Console.WriteLine($"Generated query: { users.GetQuery }");
            Console.WriteLine("-------------------");

            Console.Read();
        }
    }
}
