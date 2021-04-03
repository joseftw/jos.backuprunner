using System.ComponentModel.DataAnnotations;

namespace JOS.BackupRunner.Backup.josef.codes
{
    public class JosefCodesBackupOptions
    {
        [Required]
        public string Host { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string SshKeyPath { get; set; }
        public int Port { get; set; } = 22;
        [Required]
        public MysqlOptions Mysql { get; set; }
    }

    public class MysqlOptions
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string DatabaseName { get; set; }
    }
}
