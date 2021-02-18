using ORM;
using System;
using System.Collections.Generic;
using DatabaseUpdater.Resources;
using System.Linq;

namespace ORMDatabaseUpdater
{
    class Program
    {
        private const string Tab = "\t";

        // @Todo: use appsettings.json.
        private static readonly string ConnectionString = "Server=localhost; Trusted_Connection=True; MultipleActiveResultSets=true";

        private static string SelectedDatabase { get; set; }

        private static List<string> DatabaseList { get; set; }

        static void Main(string[] _)
        {
            SelectDatabase();

            Console.WriteLine(string.Format(Resources.SelectedDatabase_Description, SelectedDatabase));

            Console.WriteLine(Resources.Help_Description);

            while (true)
            {
                string command = Console.ReadLine().ToLower();

                switch (command.Split(' ')[0])
                {
                    case Commands.Update:
                        ParseUpdateCommand(command);
                        break;
                    case Commands.Help:
                        Console.WriteLine(Tab + Resources.Command_Update_TableName_Description);
                        Console.WriteLine(Tab + Resources.Command_Update_All_Description);
                        Console.WriteLine(Tab + Resources.Command_Help_Description);
                        Console.WriteLine(Tab + Resources.Command_Clear_Description);
                        Console.WriteLine(Tab + Resources.Command_Exit_Description);
                        break;
                    case Commands.Clear:
                        Console.Clear();
                        break;
                    case Commands.Exit:
                        Environment.Exit(Environment.ExitCode);
                        break;
                    default:
                        Console.WriteLine(string.Format(Resources.UnknownCommend_Description, command));
                        break;
                }
            }
        }

        private static void SelectDatabase()
        {
            DatabaseList = ORMUtilities.GetDatabaseList(ConnectionString);

            Console.WriteLine(string.Format(Resources.SelectDatabase_Description, DatabaseList.Count - 1));

            for (int i = 0; i < DatabaseList.Count; i++)
            {
                Console.WriteLine(string.Format(Resources.Select_Database_Format, i, DatabaseList[i]));
            }

            while (true)
            {
                string command = Console.ReadLine();
                if (int.TryParse(command, out int index) && DatabaseList.ElementAtOrDefault(index) != null)
                {
                    SelectedDatabase = DatabaseList[index];
                    return;
                }
                else
                {
                    Console.WriteLine(string.Format(Resources.SelectDatabase_Description, DatabaseList.Count - 1));
                }
            }
        }

        private static void ParseUpdateCommand(string command)
        {
            var commands = command.Split(' ');

            switch (commands.Length)
            {
                case 1:
                default:
                    Console.WriteLine(string.Format(Resources.Command_Update_InvalidParameter, commands[0]));
                    return;
                case 2:
                    // @Todo
                    throw new NotImplementedException();
            }
        }
    }
}