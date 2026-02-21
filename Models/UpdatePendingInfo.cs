namespace AccesClientWPF.Models
{
    public sealed class UpdatePendingInfo
    {
        public string ZipPath { get; set; } = "";
        public string InstallDir { get; set; } = "";
        public string TargetExePath { get; set; } = "";
        public int OriginalPid { get; set; }
        public string RemoteVersion { get; set; } = "";
        public bool RequireAdmin { get; set; }
    }
}