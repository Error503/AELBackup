using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using CommandLine;

namespace AELBackup
{
    // BUG: 'Priming' system appears to cause issues. Backup creation fails. It appears all events are happening twice
    // FIX: Backups were not being deleted, fixed already
    public class BackupManager : IDisposable
    {
        public static readonly string SaveDataDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KoeiTecmo", "Atelier Escha and Logy DX", "SAVEDATA");

        private const string SystemSaveDataName = "SYSDATA.pcsave";

        private readonly ManagerOptions _options;
        private readonly FileSystemWatcher _watcher;

        private Process _gameProcess;

        private List<string> _currentBackups;

        private bool _isPrimed;
        private bool _isFirstEvent;

        public BackupManager(ManagerOptions options)
        {
            _options = options;
            _watcher = new FileSystemWatcher
            {
                Path = SaveDataDirectory,
                Filter = "*.pcsave",
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = false
            };

            _watcher.Changed += HandleBackupTrigger;
            _watcher.Created += HandleBackupTrigger;
        }

        public void Dispose()
        {
            _watcher.Changed -= HandleBackupTrigger;
            _watcher.Created -= HandleBackupTrigger;

            _watcher?.Dispose();
            _gameProcess?.Dispose();
        }

        public void Run()
        {
            // Check that the backup directory exists
            if (!Directory.Exists(Path.Combine(SaveDataDirectory, _options.BackupDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(SaveDataDirectory, _options.BackupDirectory));
            }

            // Get the existing backups, ordered by date descending (newest first)
            _currentBackups = Directory.EnumerateFiles(Path.Combine(SaveDataDirectory, _options.BackupDirectory), "*.backup", SearchOption.TopDirectoryOnly)
                                       .OrderByDescending(File.GetCreationTimeUtc).ToList();

            WriteLineInColor($"Found {_currentBackups.Count} existing backups", ConsoleColor.Green);

            WriteLineInColor("Attempting to launch game...", ConsoleColor.Cyan);

            // Start listening
            _watcher.EnableRaisingEvents = true;

            _gameProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.OriginalLauncherPath
                }
            };

            // Start the process
            _gameProcess.Start();

            // Wait for the game to close
            while (!_gameProcess.HasExited) { }

            WriteLineInColor("Game launched. Ready to backup system data.", ConsoleColor.Green);
            Console.WriteLine("Press 'q' to close the backup application");

            while (Console.Read() != 'q') { }

            Console.WriteLine("Closing backup program");
            _watcher.EnableRaisingEvents = false;
        }

        private void HandleBackupTrigger(object source, FileSystemEventArgs args)
        {
            WriteLineInColor($"Responding to `{args.ChangeType}` event on `{args.Name}`", ConsoleColor.Cyan);

            var cachedIsFirstEvent = _isFirstEvent;
            _isFirstEvent = false;

            // Prime for changes when a game is saved
            if (!args.Name.StartsWith("SYSDATA"))
            {
                _isPrimed = true;
                return;
            }

            // This is a change to the system data file
            // If the change was primed,
            if (_isPrimed)
            {
                // This is a normal change
                CreateBackup();
                _isPrimed = false;
            }
            else
            {
                // This change was not primed by a save data change,
                // If this is the first event,
                if (cachedIsFirstEvent)
                {
                    WriteLineInColor($"Detected update to {SystemSaveDataName} file without a save data change. Restoring system data.", ConsoleColor.Red);

                    // The first event is always a read of the SYSDATA, the file may have just been corrupted
                    if (!RestoreBackup(0))
                    {
                        WriteLineInColor("Failed to restore backup automatically!", ConsoleColor.DarkRed);
                    }
                }
                else
                {
                    WriteLineInColor("Change to system data without save data change detected! Please note when this happened!", ConsoleColor.Magenta);
                    CreateBackup();
                }
            }
        }

        private void CreateBackup()
        {
            var currentDate = DateTime.UtcNow;

            var backupFileName = $"SYSDATA_{currentDate:yyyy_MM_dd_HH_mm_ss}.pcsave.backup";
            var backupFilePath = Path.Combine(SaveDataDirectory, _options.BackupDirectory, backupFileName);

            // Make sure a file with the same name does not already exist
            if (File.Exists(backupFilePath))
                return;

            // Wait for the file to be ready
            var filePath = Path.Combine(SaveDataDirectory, SystemSaveDataName);
            var counter = 10;
            while (counter > 0 && !IsFileReady(filePath))
            {
                counter--;
                Thread.Sleep(500);
            }

            if (counter == 0)
            {
                WriteLineInColor("Failed to create backup. The system data file remained in use for longer than expected", ConsoleColor.Red);
                return;
            }

            // Disable events
            _watcher.EnableRaisingEvents = false;

            // Create the backup
            File.Copy(Path.Combine(SaveDataDirectory, SystemSaveDataName), backupFilePath);

            WriteLineInColor($"Created backup `{backupFileName}`", ConsoleColor.Green);

            // Enable events
            _watcher.EnableRaisingEvents = true;

            // Insert into the backup list
            _currentBackups.Insert(0, filePath);

            // Remove old backups
            RemoveOldBackups();
        }

        private void RemoveOldBackups()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            while (_currentBackups.Count > _options.BackupCount)
            {
                var toRemove = _currentBackups[^1];

                if (File.Exists(toRemove))
                {
                    Console.WriteLine($"Deleting expired backup `{Path.GetFileName(toRemove)}`");
                    File.Delete(toRemove);
                }

                _currentBackups.RemoveAt(_currentBackups.Count - 1);
            }

            Console.ResetColor();
        }

        private bool RestoreBackup(int backupNumber)
        {
            if (backupNumber < 0 || backupNumber >= _currentBackups.Count)
                return false;

            var toRestore = _currentBackups[backupNumber];

            // Wait for the file to be ready
            var filePath = Path.Combine(SaveDataDirectory, SystemSaveDataName);
            var counter = 10;
            while (counter > 0 && !IsFileReady(filePath))
            {
                counter--;
                Thread.Sleep(500);
            }

            if (counter == 0)
            {
                WriteLineInColor("Failed to restore backup. The file remained in use for longer than expected.", ConsoleColor.Red);
                return false;
            }

            // Disable events
            _watcher.EnableRaisingEvents = false;

            WriteLineInColor($"Restored system data from `{Path.GetFileName(toRestore)}`", ConsoleColor.Cyan);

            // Enable events
            _watcher.EnableRaisingEvents = true;

            return true;
        }

        private static bool IsFileReady(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                return stream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void WriteLineInColor(string value, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class ManagerOptions
        {
            [Option('d', "directory", Default = "backups", Required = false, HelpText = "The folder for backup data")]
            public string BackupDirectory { get; set; }

            [Option('c', "count", Default = 5, Required = false, HelpText = "The number of backups to keep")]
            public int BackupCount { get; set; }

            [Option('g', "game", Required = false, HelpText = "The full path to the original game launcher")]
            public string OriginalLauncherPath { get; set; }
        }
    }
}
