using System;
using DatabaseUpdater.Resources;

namespace ORMDatabaseUpdater
{
    class Program
    {
        private const string Tab = "\t";

        // @Todo: use appsettings.json.
        private static readonly string ConnectionString = "Server=localhost; Trusted_Connection=True; MultipleActiveResultSets=true";

        private static string SelectedDatabase;

        static void Main(string[] _)
        {
            DatabaseUtilities.SelectDatabase(ConnectionString, ref SelectedDatabase);

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
                                DatabaseUtilities.UpdateDatabase();
                            }
                            // Update specific table
                            else
                            {
                                DatabaseUtilities.UpdateTable(command);
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