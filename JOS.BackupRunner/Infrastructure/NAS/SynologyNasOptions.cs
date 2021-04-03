using System;
using System.ComponentModel.DataAnnotations;

namespace JOS.BackupRunner.Infrastructure.NAS
{
    public class SynologyNasOptions
    {
        [Required]
        public Uri BaseAddress { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
