using System.Threading;
using System.Threading.Tasks;

namespace JOS.BackupRunner.Backup
{
    public interface IBackupTask
    {
        string Name { get; }
        Task Run(CancellationToken cancellationToken);
        Task Cleanup(CancellationToken cancellationToken);
    }
}