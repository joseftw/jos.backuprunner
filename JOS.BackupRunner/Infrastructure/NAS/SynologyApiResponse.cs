namespace JOS.BackupRunner.Infrastructure.NAS
{
    public class SynologyApiResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; }
    }
}
