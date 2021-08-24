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
            #region Init
            IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();

            _ = new ORMInitialize(configuration);
            #endregion

            var user = DatabaseUtilities.Update<User>(1, (x => x.DateCreated, DateTime.Now));
            // Todo:
            // Unit tests
            // multiple primary keys support
            // Niet meer hardcoded in Update functie bij ORMEntity
            // Light version van UpdateIsDirtyList voor fix (toekomstige) bugs bij update velden.

            Console.Read();
        }

#pragma warning disable IDE0051
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
#pragma warning restore IDE0051
    }
}
