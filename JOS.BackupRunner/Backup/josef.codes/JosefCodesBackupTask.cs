using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Serilog;

namespace JOS.BackupRunner.Backup.josef.codes
{
    public class JosefCodesBackupTask : BackupTask, IDisposable
    {
        private readonly JosefCodesBackupOptions _josefCodesBackupOptions;
        private readonly SshClient _sshClient;
        private readonly SftpClient _sftpClient;

        public JosefCodesBackupTask(
            IOptions<JosefCodesBackupOptions> josefCodesOptions,
            ILogger<JosefCodesBackupTask> logger) : base("josef.codes", logger)
        {
            _josefCodesBackupOptions = josefCodesOptions?.Value ?? throw new ArgumentNullException(nameof(josefCodesOptions));
            var privateKeyFile = new PrivateKeyFile(_josefCodesBackupOptions.SshKeyPath);
            _sshClient = new SshClient(
                _josefCodesBackupOptions.Host,
                _josefCodesBackupOptions.Port,
                _josefCodesBackupOptions.Username,
                privateKeyFile);
            _sftpClient = new SftpClient(
                _josefCodesBackupOptions.Host,
                _josefCodesBackupOptions.Username,
                privateKeyFile);
        }

        public override async Task Run(CancellationToken cancellationToken)
        {
            _sshClient.Connect();

            var site = "josef.codes";
            var backupFolderName = $"{DateTime.UtcNow:yyyyMMdd}";
            var backupTargetLocation = $"/tmp/{site}/{backupFolderName}";

            var createBackupTargetCommand = $"mkdir -p {backupTargetLocation}";
            RunCommand(_sshClient, createBackupTargetCommand, "Creating backup target folder...");

            var mysqlDumpCommand = $"mysqldump -u {_josefCodesBackupOptions.Mysql.Username} {_josefCodesBackupOptions.Mysql.DatabaseName} > {backupTargetLocation}/{_josefCodesBackupOptions.Mysql.DatabaseName}.sql --no-tablespaces";
            RunCommand(_sshClient, mysqlDumpCommand, "Dumping MYSQL database...");

            var josefCodesBackupCommand = $"cp -a /var/www/{site} {backupTargetLocation}";
            RunCommand(_sshClient, josefCodesBackupCommand, "Copying site to backup target folder...");

            var zipFilename = $"{site}.{backupFolderName}.zip";
            var zipBackupFileLocation = $"/tmp/{site}/{zipFilename}";
            var zipBackupTargetFolderCommand = $"zip -r {zipBackupFileLocation} {backupTargetLocation}";
            RunCommand(_sshClient, zipBackupTargetFolderCommand, "Creating zip file...");
            
            _sshClient.Disconnect();

            _sftpClient.Connect();
            var path = @$"\\jos-nas\backup\josef.codes\{backupFolderName}";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filePath = @$"{path}\{zipFilename}";
            var fileStream = File.OpenWrite(filePath);
            Logger.LogInformation("Downloading file {Filename}...", zipFilename);
            _sftpClient.DownloadFile(zipBackupFileLocation, fileStream);
            Logger.LogInformation("{Filename} downloaded", zipFilename);
            await fileStream.FlushAsync(cancellationToken);
            await fileStream.DisposeAsync();
            _sftpClient.Disconnect();
        }

        public override Task Cleanup(CancellationToken cancellationToken)
        {
            _sshClient.Connect();
            RunCommand(_sshClient, "rm -rf /tmp/josef.codes", "Removing temporary files");
            _sshClient.Disconnect();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _sshClient?.Dispose();
            _sftpClient?.Dispose();
        }
    }
}
