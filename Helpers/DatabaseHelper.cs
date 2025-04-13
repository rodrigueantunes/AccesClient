using System;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using AccesClientWPF.Models;

namespace AccesClientWPF.Helpers
{
    public static class DatabaseHelper
    {
        private static readonly string JsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        public static DatabaseModel LoadDatabase()
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(JsonFilePath))
                {
                    var jsonData = File.ReadAllText(JsonFilePath);
                    var database = JsonConvert.DeserializeObject<DatabaseModel>(jsonData);
                    return database ?? new DatabaseModel();  // Si le fichier est vide ou mal formaté, retourner un modèle vide
                }
                else
                {
                    // Si le fichier n'existe pas, retourner un modèle vide
                    return new DatabaseModel();
                }
            }
            catch (Exception ex)
            {
                // Si une erreur se produit, afficher l'exception et retourner un modèle vide
                Console.WriteLine($"Erreur lors du chargement de la base de données : {ex.Message}");
                return new DatabaseModel();
            }
        }

        public static void SaveDatabase(DatabaseModel database)
        {
            try
            {
                // Vérifier que la base de données n'est pas nulle
                if (database != null)
                {
                    // Convertir les données en JSON et les écrire dans le fichier
                    var jsonData = JsonConvert.SerializeObject(database, Formatting.Indented);
                    File.WriteAllText(JsonFilePath, jsonData);
                }
                else
                {
                    Console.WriteLine("La base de données est vide, aucune donnée à sauvegarder.");
                }
            }
            catch (Exception ex)
            {
                // Si une erreur se produit lors de l'écriture dans le fichier
                Console.WriteLine($"Erreur lors de la sauvegarde de la base de données : {ex.Message}");
            }
        }
    }

    public class DatabaseModel
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> Files { get; set; } = new();
    }
}
