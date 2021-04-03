using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JOS.BackupRunner.Backup
{
    public class BackupRunner
    {
        private readonly IBackupTask[] _backupTasks;
        private readonly ILogger<BackupRunner> _logger;

        public BackupRunner(IEnumerable<IBackupTask> backupTasks, ILogger<BackupRunner> logger)
        {
            _backupTasks = backupTasks?.ToArray() ?? throw new ArgumentNullException(nameof(backupTasks));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Found {NumberOfBackupTasks} backup tasks", _backupTasks.Length);
            for (var i = 1; i < _backupTasks.Length + 1; i++)
            {
                _logger.LogInformation("{BackupTaskOrder}. {BackupTask}", i, _backupTasks[i-1].Name);
            }
            foreach (var backupTask in _backupTasks)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Starting {BackupTask}...", backupTask.Name);
                    await backupTask.Run(cancellationToken);
                    _logger.LogInformation("Starting cleanup for {BackupTask}", backupTask.Name);
                    await backupTask.Cleanup(cancellationToken);
                    stopwatch.Stop();
                    _logger.LogInformation("{BackupTask} done, took {ElapsedTime}", backupTask.Name, stopwatch.Elapsed);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error when running {BackupTask} after {ElapsedTime}", backupTask.Name, stopwatch.Elapsed);
                }
            }
        }
    }
}
