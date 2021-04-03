using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace JOS.BackupRunner.Backup.Unifi
{
    public class UdmpBackupTask : BackupTask
    {
        private const int VersionsToKeep = 2;
        private readonly ScpClient _scpClient;
        private readonly SshClient _sshClient;
        private readonly UdmpBackupOptions _unifiOptions;

        public UdmpBackupTask(
            IOptions<UdmpBackupOptions> unifiOptions,
            ILogger<UdmpBackupTask> logger) : base("UDMP", logger)
        {
            _unifiOptions = unifiOptions?.Value ?? throw new ArgumentNullException(nameof(unifiOptions));
            _scpClient = new ScpClient(_unifiOptions.Host, _unifiOptions.Username, _unifiOptions.Password);
            _sshClient = new SshClient(_unifiOptions.Host, _unifiOptions.Username, _unifiOptions.Password);
        }

        public override Task Run(CancellationToken cancellationToken)
        {
            var backupFolderName = $"{DateTime.UtcNow:yyyyMMdd}";
            var path = @$"\\jos-nas\backup\udmp\{backupFolderName}";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var unifiBackupLocation = "/data/unifi/data/backup/autobackup";
            _sshClient.Connect();
            var files = RunCommand(_sshClient, $"ls {unifiBackupLocation}").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            _sshClient.Disconnect();
            _scpClient.ErrorOccurred += (sender, args) => Logger.LogError(args.Exception, "Error when downloading files");
            Logger.LogInformation("Starting to download {NumberOfFiles} files", files.Length);
            _scpClient.Connect();

            Parallel.ForEach(
                files,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 },
                file =>
                {
                    _scpClient.Download($"{unifiBackupLocation}/{file}", File.OpenWrite(@$"{path}\{file}"));
                });

            _scpClient.Disconnect();
            return Task.CompletedTask;
        }

        public override Task Cleanup(CancellationToken cancellationToken)
        {
            var backupLocation = @"\\jos-nas\backup\udmp";
            var foldersToRemove = Directory.GetDirectories(backupLocation)
                .Select(Path.GetFileName)
                .Select(x => DateTime.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture))
                .OrderByDescending(x => x)
                .Skip(VersionsToKeep)
                .ToArray();
            
            Logger.LogInformation("Found {NumberOfFoldersToRemove} folders to remove", foldersToRemove.Length);

            foreach (var folder in foldersToRemove)
            {
                var path = $@"{backupLocation}\{folder:yyyyMMdd}";
                Logger.LogInformation("Removing {FolderName}...", path);
                Directory.Delete(path, recursive: true);
                Logger.LogInformation("{FolderName} removed", path);
            }

            return Task.CompletedTask;
        }
    }
}
