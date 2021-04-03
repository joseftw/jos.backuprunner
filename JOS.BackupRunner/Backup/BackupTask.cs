using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace JOS.BackupRunner.Backup
{
    public abstract class BackupTask : IBackupTask
    {
        protected ILogger<BackupTask> Logger;
        protected BackupTask(string name, ILogger<BackupTask> logger)
        {
            Name = name;
            Logger = logger;
        }

        public string Name { get; }
        public abstract Task Run(CancellationToken cancellationToken);
        public abstract Task Cleanup(CancellationToken cancellationToken);

        protected string RunCommand(SshClient sshClient, string command)
        {
            return RunCommand(sshClient, command, null);
        }

        protected string RunCommand(SshClient sshClient, string command, string description)
        {
            Logger.LogInformation("Executing: {Action}", description ?? command);
            var stopwatch = Stopwatch.StartNew();
            var result = sshClient.RunCommand(command);

            if (result.ExitStatus != 0)
            {
                Logger.LogError(result.Error);
                return result?.Result;
            }

            stopwatch.Stop();
            Logger.LogInformation("Done, took {Elapsed}", stopwatch.Elapsed);
            return result.Result;
        }
    }
}
