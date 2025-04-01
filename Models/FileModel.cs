﻿namespace AccesClientWPF.Models
{
    public class FileModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string FullPath { get; set; }
        public string Client { get; set; }
    }

    public enum FileType
    {
        RDS,     // Bureau à distance
        VPN,
        AnyDesk,
        Dossier  // Nouveau type pour les dossiers
    }
}