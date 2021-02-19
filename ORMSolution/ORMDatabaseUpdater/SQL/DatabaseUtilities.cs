using ORM;
using System;
using System.Linq;
using ORM.Attributes;
using DatabaseUpdater.Resources;

namespace ORMDatabaseUpdater
{
    internal abstract class DatabaseUtilities
    {
        internal static void SelectDatabase(string connectionString, ref string selectedDatabase)
        {
            var databaseList = ORMUtilities.GetDatabaseList(connectionString);

            Console.WriteLine(string.Format(Resources.SelectDatabase_Description, databaseList.Count - 1));

            for (int i = 0; i < databaseList.Count; i++)
            {
                Console.WriteLine(string.Format(Resources.Select_Database_Format, i, databaseList[i]));
            }

            while (true)
            {
                string command = Console.ReadLine();
                if (int.TryParse(command, out int index) && databaseList.ElementAtOrDefault(index) != null)
                {
                    selectedDatabase = databaseList[index];
                    return;
                }
                else
                {
                    Console.WriteLine(string.Format(Resources.SelectDatabase_Description, databaseList.Count - 1));
                }
            }
        }

        internal static void UpdateDatabase()
        {
            // @Todo
            Console.WriteLine("Entire database has been updated");
        }

        internal static void UpdateTable(string tableName)
        {
            var entities = ORMReflectionUtilities.GetAllEntities();

            if (entities.Any(x => (x.GetCustomAttributes(typeof(ORMTableAttribute), true).FirstOrDefault() as ORMTableAttribute).TableName.Equals(tableName, StringComparison.InvariantCultureIgnoreCase)))
            {
                // @Todo
                Console.WriteLine($"Table '{tableName}' has been updated.");
            }
            else
            {
                // @Todo
                Console.WriteLine($"Couldn't find the table '{tableName}' in the database, please check your spelling.");
            }
        }
    }
}
