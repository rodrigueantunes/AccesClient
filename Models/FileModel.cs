using System;
using Newtonsoft.Json;

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

        // ✅ IMPORTANT :
        // Le JSON actuel contient "Username": null et "Password": null
        // Si ces props sont désérialisées, elles écrasent WindowsUsername/WindowsPassword via le proxy.
        [JsonIgnore]
        public string Username
        {
            get => WindowsUsername;
            set
            {
                // sécurité si jamais quelqu'un l'utilise en code
                if (value != null) WindowsUsername = value;
            }
        }

        [JsonIgnore]
        public string Password
        {
            get => WindowsPassword;
            set
            {
                if (value != null) WindowsPassword = value;
            }
        }

        public string RangementParent { get; set; }
    }

public enum FileType
    {
        RDS,
        VPN,
        AnyDesk,
        Dossier,
        Fichier,
        MotDePasse,
        Rangement
    }
}