using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AELBackup
{
    public class Program
    {
        private static readonly string DefaultListenDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KoeiTecmo", "Atelier Escha and Logy DX", "SAVEDATA");

        private const string TargetFileName = "SYSDATA.pcsave";

        private static int _backupCount = 5;
        private static string _backupsFolder = Path.Combine(DefaultListenDir, "backups");

        private static FileSystemWatcher _watcher;
        private static List<string> _currentBackups;

        public static void Main(string[] args)
        {
            Console.WriteLine("Atelier Escha & Logy System Data Backup Program is running...");

            if (args != null && args.Length > 0)
            {
                ReadCommandLineArgs(args);
            }

            // Check for backups directory
            if (!Directory.Exists(_backupsFolder))
            {
                Directory.CreateDirectory(_backupsFolder);
            }

            // Get the current backups ordered by date descending (Newest first)
            _currentBackups = Directory.EnumerateFiles(_backupsFolder, "*.backup", SearchOption.TopDirectoryOnly).OrderByDescending(File.GetCreationTimeUtc).ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Found {_currentBackups.Count} existing backups");
            Console.ResetColor();

            // Create the file system watcher
            _watcher = new FileSystemWatcher(DefaultListenDir)
            {
                Filter = TargetFileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            // Bind events
            _watcher.Created += CreateBackup;
            _watcher.Changed += CreateBackup;
            _watcher.Deleted += CreateBackup;

            // Start watching
            _watcher.EnableRaisingEvents = true;

            // TODO: Have ability to enter number to restore that backup
            // Loop until told to exit
            Console.WriteLine("Press 'q' to close the application.\n");
            while (Console.Read() != 'q') { }

            Console.WriteLine("Closing application");

            _watcher.EnableRaisingEvents = false;

            // Remove event listeners
            _watcher.Created -= CreateBackup;
            _watcher.Changed -= CreateBackup;
            _watcher.Deleted -= CreateBackup;

            _watcher.Dispose();

            Console.WriteLine("Application closed");
        }

        private static void ReadCommandLineArgs(string[] args)
        {
            throw new NotImplementedException("Command line arguments not yet implemented");
        }

        private static void CreateBackup(object sender, FileSystemEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Triggering backup of system data from `{args.ChangeType}` on file `{args.Name}`");

            var targetFilePath = Path.Combine(DefaultListenDir, TargetFileName);
            var safetyCounter = 25;
            while (safetyCounter > 0 && !IsFileReady(targetFilePath))
            {
                safetyCounter--;
            }

            if (safetyCounter == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to create backup, the file remained in use for longer than expected");
                Console.ResetColor();
                return;
            }

            var currentDate = DateTime.Now;

            var backupFileName = $"SYSDATA_{currentDate:yyyy_MM_dd_HH_mm_ss}.pcsave.backup";
            var savePath = Path.Combine(_backupsFolder, backupFileName);

            // First, check if a file with the same name already exists, this removes duplicate calls
            if (File.Exists(savePath))
                return;

            // Disable raising of events
            _watcher.EnableRaisingEvents = false;

            // Copy the file
            File.Copy(targetFilePath, savePath);

            // Reenable raising of events
            _watcher.EnableRaisingEvents = true;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Created backup: `{savePath}`");

            // Insert into the front of the list
            _currentBackups.Insert(0, savePath);

            // Remove any extra backups
            while (_currentBackups.Count > _backupCount)
            {
                var toRemove = _currentBackups[^1];

                // Check if the file exists before deleting it first
                if (File.Exists(toRemove))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Deleting expired backup: `{toRemove}`");
                    File.Delete(toRemove);
                }

                // Remove the last entry in the list
                _currentBackups.RemoveAt(_currentBackups.Count - 1);
            }

            Console.ResetColor();
        }

        private static bool IsFileReady(string filePath)
        {
            try
            {
                using var inputStream = File.OpenRead(filePath);

                return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
