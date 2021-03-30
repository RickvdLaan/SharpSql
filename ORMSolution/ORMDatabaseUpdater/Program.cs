using System;
using DatabaseUpdater.Resources;
using Microsoft.Extensions.Configuration;
using ORM;

namespace ORMDatabaseUpdater
{
    class Program
    {
        private const string Tab = "\t";

        private static string SelectedDatabase;

        static void Main()
        {
            #region Init
            IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();

            _ = new ORMInitialize(configuration);
            #endregion

            Utilities.SelectDatabase(ref SelectedDatabase);

            DatabaseUtilities.OverrideConnectionString($"Server=localhost; Database={ SelectedDatabase }; Trusted_Connection=True; MultipleActiveResultSets=true");

            Console.WriteLine(string.Format(Resources.SelectedDatabase_Description, SelectedDatabase));

            Console.WriteLine(Resources.Help_Description);

            while (true)
            {
                string command = Console.ReadLine().ToLower();

                switch (command.Split(' ')[0])
                {
                    case Commands.Update:
                        command = ParseUpdateCommand(command);

                        if (!string.IsNullOrEmpty(command))
                        {
                            // Update all tables
                            if (command.Equals(Commands.Update_All, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Utilities.UpdateDatabase();
                            }
                            // Update specific table
                            else
                            {
                                Utilities.UpdateTable(command);
                            }
                        }
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

        private static string ParseUpdateCommand(string command)
        {
            var commands = command.Split(' ');

            if (commands.Length == 1)
            {
                Console.WriteLine(string.Format(Resources.Command_Update_InvalidParameter, commands[0]));
                return null;
            }

            return commands[1];
        }
    }
}