namespace AccesClientWPF.ViewModels
{
    public sealed class UpdatePromptViewModel
    {
        public UpdatePromptViewModel(string localVersion, string remoteVersion, string sourcePath)
        {
            LocalVersion = localVersion;
            RemoteVersion = remoteVersion;
            SourcePath = sourcePath;
        }

        public string LocalVersion { get; }
        public string RemoteVersion { get; }
        public string SourcePath { get; }

        public string Title => "Mise à jour disponible";
        public string Message =>
            $"Une nouvelle version est disponible.\n\n" +
            $"Version installée : {LocalVersion}\n" +
            $"Nouvelle version : {RemoteVersion}\n\n" +
            $"Source : {SourcePath}\n\n" +
            $"Voulez-vous mettre à jour maintenant ?";
    }
}