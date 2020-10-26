using System;
using System.IO;

using CommandLine;

namespace AELBackup
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program
    {
        private static BackupManager _manager;

        public static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Atelier Escha & Logy DX Steam Edition System Data Backup Program is running");
            Console.WriteLine("This program is developed and maintained independently and is not affiliated with KoeiTecmo or Gust.");
            Console.WriteLine("For support on this backup system, visit https://github.com/Error503/AELBackup");
            Console.ResetColor();

            // Parse options
            Parser.Default.ParseArguments<BackupManager.ManagerOptions>(args).WithParsed(ProcessOptions);

            using (_manager)
            {
                _manager.Run();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Atelier Escha & Logy DX Steam Edition System Data Backup Program closing");
            Console.ResetColor();
        }

        private static void ProcessOptions(BackupManager.ManagerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.OriginalLauncherPath))
            {
                // Look for our configuration file
                var configFilePath = Path.Combine(BackupManager.SaveDataDirectory, options.BackupDirectory, "BackupSettings.txt");
                if (!File.Exists(configFilePath))
                {
                    // Config file does not exist, so prompt the user for the launcher directory
                    string filePath;
                    bool isValid;
                    do
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("First time setup. Please enter the full file path of the game executable.\n>>> ");
                        Console.ResetColor();

                        filePath = Console.ReadLine() ?? string.Empty;
                        isValid = File.Exists(filePath) && filePath.EndsWith(".exe");
                        if (!isValid)
                        {
                            Console.WriteLine("Invalid file path. Please make sure the file exists and is an executable (.exe) file");
                        }
                    } while (!isValid);

                    options.OriginalLauncherPath = filePath;

                    // Write the file path to the configuration file for next launch
                    using var writer = new StreamWriter(File.Create(configFilePath));
                    writer.WriteLine(filePath);
                }
                else
                {
                    options.OriginalLauncherPath = File.ReadAllText(configFilePath);
                }
            }

            // Create the manager
            _manager = new BackupManager(options);
        }
    }
}
