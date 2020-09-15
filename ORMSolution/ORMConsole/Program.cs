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

            new ORMInitialize(configuration);
            #endregion

            // A few cases for the initial Update Unit Tests.

            // Case 1
            //var user1 = new User(1)
            //{
            //    Password = "fjkldsfj",
            //};

            //user1.Save();

            // Case 2
            //var organisation2 = new Organisation(1);

            //var user2 = new User(1)
            //{
            //    Password = "fjkldsfj",
            //    Organisation = organisation2

            //};

            //user2.Save();

            // Case 3
            //var organisation3 = new Organisation(1)
            //{
            //    Name = "Nieuwe naam"
            //};

            //var user3 = new User(1)
            //{
            //    Password = "fjkldsfj",
            //    Organisation = organisation3
            //};

            //user3.Save();

            // Case 4
            //var user4 = new User(1)
            //{
            //    Password = "fjkldsfj",
            //    Organisation = new Organisation() { Name = "IkBenNieuw" }
            //};

            //user4.Save();

            // Case 5
            //var user5 = new User(1)
            //{
            //    Password = "fjkldsfj",
            //    // Organisation was already null.
            //    Organisation = null
            //};

            //user5.Save();

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
