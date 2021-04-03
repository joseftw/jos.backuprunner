using JOS.BackupRunner.Backup.josef.codes;
using JOS.BackupRunner.Backup.Unifi;
using Microsoft.Extensions.DependencyInjection;

namespace JOS.BackupRunner.Backup
{
    public static class BackupServiceCollectionExtensions
    {
        public static void AddBackupFeature(this IServiceCollection services)
        {
            services.AddSingleton<BackupRunner>();

            services.AddJosefCodesBackupTask();
            services.AddUdmpBackupTask();
        }

        private static void AddJosefCodesBackupTask(this IServiceCollection services)
        {
            services.AddOptions<JosefCodesBackupOptions>().BindConfiguration("backupTasks:josef.codes").ValidateDataAnnotations();
            services.AddSingleton<IBackupTask, JosefCodesBackupTask>();
        }

        private static void AddUdmpBackupTask(this IServiceCollection services)
        {
            services.AddOptions<UdmpBackupOptions>().BindConfiguration("backupTasks:udmp").ValidateDataAnnotations();
            services.AddSingleton<IBackupTask, UdmpBackupTask>();
        }
    }
}