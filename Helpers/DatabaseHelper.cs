using System;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using AccesClientWPF.Models;

namespace AccesClientWPF.Helpers
{
    public static class DatabaseHelper
    {
        private const string JsonFilePath = @"C:\Application\database.json";

        public static DatabaseModel LoadDatabase()
        {
            if (File.Exists(JsonFilePath))
            {
                var jsonData = File.ReadAllText(JsonFilePath);
                return JsonConvert.DeserializeObject<DatabaseModel>(jsonData) ?? new DatabaseModel();
            }
            return new DatabaseModel();
        }

        public static void SaveDatabase(DatabaseModel database)
        {
            File.WriteAllText(JsonFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
        }
    }

    public class DatabaseModel
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> Files { get; set; } = new();
    }
}
