using Microsoft.Extensions.Configuration;
using ORM;
using System;
using NorthwindDAL.Entities;
using NorthwindDAL.Collections;
using System.Linq;

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

            Customers customers = new Customers().Fetch() as Customers;
            Customer customer = customers.FirstOrDefault();

            Console.Read();
        }
    }
}
