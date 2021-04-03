using System.ComponentModel.DataAnnotations;

namespace JOS.BackupRunner.Backup.Unifi
{
    public class UdmpBackupOptions
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Host { get; set; }
    }
}
