using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccesClientWPF.Models;
using Newtonsoft.Json;

namespace AccesClientWPF.Views
{
    public partial class AddEntryWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private ObservableCollection<FileModel> _files = new();
        private ObservableCollection<ClientModel> _clients;
        private FileModel _editingFile;

        public FileModel FileEntry { get; private set; }

        // Constructeur pour l'ajout d'un élément
        public AddEntryWindow(ObservableCollection<ClientModel> clients, ClientModel selectedClient = null)
        {
            InitializeComponent();
            _clients = clients;
            CmbClient.ItemsSource = _clients;
            CmbClient.SelectedItem = selectedClient;
            CmbClient.IsEnabled = false; // Rendre le champ client non modifiable
            LoadFiles();
        }

        // Constructeur pour l'édition d'un élément
        public AddEntryWindow(ObservableCollection<ClientModel> clients, ClientModel selectedClient, FileModel editingFile)
            : this(clients, selectedClient)
        {
            _editingFile = editingFile;
            TxtName.Text = editingFile.Name;

            // Convertir l'énumération en string pour le ComboBox
            CmbType.SelectedItem = CmbType.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag.ToString() == editingFile.Type.ToString());
        }

        private void LoadFiles()
        {
            if (File.Exists(_jsonFilePath))
            {
                var jsonData = File.ReadAllText(_jsonFilePath);
                var database = JsonConvert.DeserializeObject<DatabaseModel>(jsonData);
                _files = database?.Files ?? new ObservableCollection<FileModel>();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || CmbType.SelectedItem == null || CmbClient.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedType = (CmbType.SelectedItem as ComboBoxItem)?.Tag.ToString();  // Utilisation du Tag

            // Conversion de string à FileType pour correspondre avec l'énumération
            FileType typeEnum = selectedType switch
            {
                "RDS" => FileType.RDS,
                "VPN" => FileType.VPN,
                "AnyDesk" => FileType.AnyDesk,
                _ => FileType.RDS  // Valeur par défaut
            };

            // Ici on doit convertir `typeEnum` en string pour être compatible avec les autres champs
            var fileTypeString = Enum.GetName(typeof(FileType), typeEnum);

            var newEntry = new FileModel
            {
                Name = TxtName.Text,
                Type = fileTypeString,  // Assignation de la chaîne de caractères, pas de l'énumération
                FullPath = "",
                Client = ((ClientModel)CmbClient.SelectedItem).Name
            };

            if (_editingFile != null)
            {
                var index = _files.IndexOf(_editingFile);
                _files[index] = newEntry;
            }
            else
            {
                _files.Add(newEntry);
            }

            SaveFiles();
            FileEntry = newEntry;
            DialogResult = true;
            Close();
        }



        private void SaveFiles()
        {
            var database = new DatabaseModel
            {
                Clients = _clients,
                Files = _files
            };

            File.WriteAllText(_jsonFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
