namespace AccesClientWPF.Models
{
    public class FileModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string FullPath { get; set; }
        public string Client { get; set; }
        public string CustomIconPath { get; set; }
        public string WindowsUsername { get; set; }
        public string WindowsPassword { get; set; }
    }

    public enum FileType
    {
        RDS,     // Bureau à distance
        VPN,
        AnyDesk,
        Dossier,
        Fichier  // Nouveau type pour les fichiers
    }
}