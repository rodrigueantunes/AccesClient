using System;
using System.IO;
using Newtonsoft.Json;

namespace AccesClientWPF.Helpers
{
    public class AppSettings
    {
        public string AnyDeskPath { get; set; } = @"C:\Program Files (x86)\AnyDesk\AnyDesk.exe";

        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // Singleton instance
        private static AppSettings _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        // Charger les paramètres depuis un fichier JSON
        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des paramètres : {ex.Message}");
            }
            
            return new AppSettings();
        }

        // Sauvegarder les paramètres dans un fichier JSON
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde des paramètres : {ex.Message}");
            }
        }

        // Vérifier si le chemin d'AnyDesk est valide
        public bool IsAnyDeskPathValid()
        {
            return !string.IsNullOrEmpty(AnyDeskPath) && File.Exists(AnyDeskPath);
        }
    }
}