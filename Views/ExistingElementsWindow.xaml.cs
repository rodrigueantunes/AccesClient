using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AccesClientWPF.Models;
using Newtonsoft.Json;

namespace AccesClientWPF.Views
{
    public partial class ExistingElementsWindow : Window
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
        private readonly ClientModel _client;

        // ✅ NEW : support injection (Share/.antclient)
        private readonly ObservableCollection<FileModel> _injectedFiles;
        private readonly bool _useInjectedFiles;

        public string ClientName { get; set; }

        public ObservableCollection<NodeVm> Nodes { get; set; } = new();

        private NodeVm _selectedNode;

        public ExistingElementsWindow(ClientModel client, ObservableCollection<FileModel> injectedFiles = null)
        {
            InitializeComponent();

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _injectedFiles = injectedFiles;
            _useInjectedFiles = injectedFiles != null;

            ClientName = _client?.Name ?? "";
            DataContext = this;

            LoadElements();
        }

        // ----------------------------
        // Source de vérité
        // ----------------------------
        private IList<FileModel> GetSourceFilesSnapshot()
        {
            if (_useInjectedFiles)
                return (_injectedFiles ?? new ObservableCollection<FileModel>()).ToList();

            var db = LoadDatabase();
            return db.Files.ToList();
        }

        private DatabaseModel LoadDatabase()
        {
            if (!File.Exists(_jsonFilePath))
                return new DatabaseModel();

            var json = File.ReadAllText(_jsonFilePath);
            return JsonConvert.DeserializeObject<DatabaseModel>(json) ?? new DatabaseModel();
        }

        private void SaveDatabase(DatabaseModel database)
        {
            File.WriteAllText(_jsonFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
        }

        // ✅ NEW : write-back selon mode
        private void PersistChangesToSource(IEnumerable<FileModel> newSourceFiles)
        {
            if (_useInjectedFiles)
            {
                _injectedFiles.Clear();
                foreach (var f in newSourceFiles)
                    _injectedFiles.Add(f);

                return; // Share : l'appelant (AddEntry/Share VM) gère le Save .antclient
            }

            var db = new DatabaseModel { Files = new ObservableCollection<FileModel>(newSourceFiles) };
            // ⚠️ Conserve aussi la liste Clients si elle existe dans ton DatabaseModel
            // Si ton DatabaseModel a Clients, il faut la recopier :
            var existingDb = LoadDatabase();
            db.Clients = existingDb.Clients;

            SaveDatabase(db);
        }

        private void LoadElements()
        {
            Nodes.Clear();

            var all = GetSourceFilesSnapshot()
                .Where(f => string.Equals(f.Client, _client.Name, StringComparison.OrdinalIgnoreCase))
                .Where(f => !string.Equals(f.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Node Racine
            var root = new NodeVm
            {
                Display = "Racine",
                TypeText = "",
                FontWeight = FontWeights.Bold,
                IsFolder = true
            };

            foreach (var f in all.Where(x => string.IsNullOrWhiteSpace(x.RangementParent)
                                             && !string.Equals(x.Type, "Rangement", StringComparison.OrdinalIgnoreCase)))
                root.Children.Add(NodeVm.FromFile(f));

            Nodes.Add(root);

            // Nodes Rangements (type=Rangement, parent vide)
            var rangements = all
                .Where(x => string.Equals(x.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(x.RangementParent))
                .OrderBy(x => x.Name);

            foreach (var r in rangements)
            {
                var nodeR = new NodeVm
                {
                    File = r,
                    Display = r.Name,
                    TypeText = "(Rangement)",
                    FontWeight = FontWeights.Bold,
                    IsFolder = true
                };

                foreach (var child in all.Where(x => string.Equals(x.RangementParent ?? "", r.Name ?? "", StringComparison.OrdinalIgnoreCase)))
                    nodeR.Children.Add(NodeVm.FromFile(child));

                Nodes.Add(nodeR);
            }
        }

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedNode = e.NewValue as NodeVm;
        }

        private FileModel PrepareEditingFile(FileModel file)
        {
            // identique à ton ancien comportement : on évite d’afficher les mdp
            var credentials = (file.FullPath ?? "").Split(':');
            string fullPath;

            if (file.Type == "RDS" && credentials.Length >= 2)
                fullPath = $"{credentials[0]}:{credentials[1]}:"; // mdp vide
            else if (file.Type == "AnyDesk" && credentials.Length >= 1)
                fullPath = $"{credentials[0]}:"; // mdp vide
            else
                fullPath = file.FullPath;

            return new FileModel
            {
                Name = file.Name,
                Type = file.Type,
                FullPath = fullPath,
                Client = file.Client,
                CustomIconPath = file.CustomIconPath,
                WindowsUsername = file.WindowsUsername,
                WindowsPassword = file.WindowsPassword,
                RangementParent = file.RangementParent
            };
        }

        private void AddElement_Click(object sender, RoutedEventArgs e)
        {
            // Si un rangement est sélectionné, on pré-sélectionne le parent
            string defaultParent = null;
            if (_selectedNode?.IsFolder == true && _selectedNode.File != null && _selectedNode.File.Type == "Rangement")
                defaultParent = _selectedNode.File.Name;

            // ✅ IMPORTANT : on injecte la source (Share/.antclient) si présente
            var add = new AddEntryWindow(new ObservableCollection<ClientModel> { _client }, _client, editingFile: null, injectedFiles: _useInjectedFiles ? _injectedFiles : null);

            if (add.ShowDialog() == true && add.FileEntry != null)
            {
                if (!string.Equals(add.FileEntry.Type, "Rangement", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(add.FileEntry.RangementParent)
                    && !string.IsNullOrWhiteSpace(defaultParent))
                {
                    add.FileEntry.RangementParent = defaultParent;
                }

                var source = GetSourceFilesSnapshot();
                source.Add(add.FileEntry);
                PersistChangesToSource(source);

                LoadElements();
            }
        }

        private void EditElement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode?.File == null)
            {
                MessageBox.Show("Sélectionne un élément (pas un dossier Racine/Rangement).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedFile = _selectedNode.File;

            var editFile = PrepareEditingFile(selectedFile);

            // ✅ IMPORTANT : injection si Share
            var edit = new AddEntryWindow(new ObservableCollection<ClientModel> { _client }, _client, editFile, injectedFiles: _useInjectedFiles ? _injectedFiles : null);

            if (edit.ShowDialog() == true && edit.FileEntry != null)
            {
                var source = GetSourceFilesSnapshot();

                var existing = source.FirstOrDefault(f =>
                    string.Equals(f.Client, selectedFile.Client, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Type, selectedFile.Type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Name, selectedFile.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.RangementParent ?? "", selectedFile.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    var idx = source.IndexOf(existing);
                    source[idx] = edit.FileEntry;

                    PersistChangesToSource(source);
                    LoadElements();
                }
            }
        }

        private void DeleteElement_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode?.File == null)
            {
                MessageBox.Show("Sélectionne un élément à supprimer.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedFile = _selectedNode.File;

            // snapshot source (share injecté OU database.json)
            var source = GetSourceFilesSnapshot();

            // ✅ Sécurité : interdire suppression d'un rangement non vide
            if (string.Equals(selectedFile.Type, "Rangement", StringComparison.OrdinalIgnoreCase))
            {
                bool hasChildren = source.Any(f =>
                    string.Equals(f.Client, _client.Name, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(f.Type, "MotDePasse", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(f.RangementParent ?? "", selectedFile.Name ?? "", StringComparison.OrdinalIgnoreCase));

                if (hasChildren)
                {
                    MessageBox.Show(
                        "Suppression interdite : ce rangement contient des éléments.\n" +
                        "Déplace d'abord les éléments vers la racine ou un autre rangement.",
                        "Suppression interdite",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            if (MessageBox.Show($"Supprimer '{selectedFile.Name}' ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var existing = source.FirstOrDefault(f =>
                string.Equals(f.Client, selectedFile.Client, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Type, selectedFile.Type, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Name, selectedFile.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.RangementParent ?? "", selectedFile.RangementParent ?? "", StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                source.Remove(existing);
                PersistChangesToSource(source);
                LoadElements();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public class NodeVm
        {
            public FileModel File { get; set; } // null pour Racine
            public string Display { get; set; }
            public string TypeText { get; set; }
            public FontWeight FontWeight { get; set; } = FontWeights.Normal;
            public bool IsFolder { get; set; }

            public ObservableCollection<NodeVm> Children { get; set; } = new();

            public static NodeVm FromFile(FileModel f)
                => new NodeVm
                {
                    File = f,
                    Display = f?.Name ?? "",
                    TypeText = $"({f?.Type})",
                    FontWeight = FontWeights.Normal,
                    IsFolder = false
                };
        }
    }
}