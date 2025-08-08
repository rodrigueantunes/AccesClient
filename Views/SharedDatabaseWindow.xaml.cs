using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using AccesClientWPF.Models;
using AccesClientWPF.ViewModels;
using AccesClientWPF.Helpers;
using System.Windows.Media;
using Newtonsoft.Json;
using HelpersDatabaseModel = AccesClientWPF.Helpers.DatabaseModel;
using ModelsDatabaseModel = AccesClientWPF.Models.DatabaseModel;
using System.Diagnostics;

namespace AccesClientWPF.Views
{
    // Ajout de la déclaration partielle qui est probablement manquante
    public partial class SharedDatabaseWindow : Window
    {
        private SharedDatabaseViewModel _viewModel;
        private string _currentFilePath;
        private string _originalFileName;
        private Button _saveButton;
        private Button _cancelButton;
        private string _lockFilePath;
        private bool _hasLock;


        public SharedDatabaseWindow()
        {
            InitializeComponent();

            _viewModel = new SharedDatabaseViewModel();
            DataContext = _viewModel;

            this.Loaded += SharedDatabaseWindow_Loaded;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed) return typed;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void SharedDatabaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Rechercher les boutons par leur nom après le chargement de la fenêtre
            _saveButton = FindButtonByName("BtnSave");
            _cancelButton = FindButtonByName("BtnCancel");

            // Désactiver les boutons s'ils sont trouvés
            if (_saveButton != null) _saveButton.IsEnabled = false;
            if (_cancelButton != null) _cancelButton.IsEnabled = false;
        }

        // Méthode pour trouver un bouton par son nom
        private Button FindButtonByName(string name)
        {
            return FindVisualChild<Button>(this, button => button.Name == name);
        }

        // Version modifiée de FindVisualChild qui accepte un prédicat
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> predicate) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && predicate(typedChild))
                    return typedChild;

                var result = FindVisualChild<T>(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void OpenSharedDatabase_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Ouvrir une base partagée",
                Filter = "Fichiers base partagée (*.antclient)|*.antclient|Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                InitialDirectory = @"\\172.16.0.49\Partage\Volume\Administration logiciels et matériels",
                DefaultExt = ".antclient"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string selectedFilePath = openFileDialog.FileName;
                    string lockFilePath = $"{selectedFilePath}.lock";

                    // Vérifier si déjà ouvert
                    if (File.Exists(lockFilePath))
                    {
                        string lockingUser = File.ReadAllText(lockFilePath);
                        MessageBox.Show($"La base partagée est ouverte par {lockingUser}.",
                                        "Base verrouillée", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Créer le verrou
                    File.WriteAllText(lockFilePath, Environment.UserName);
                    _lockFilePath = lockFilePath;
                    _hasLock = true;

                    _currentFilePath = selectedFilePath;
                    _originalFileName = Path.GetFileNameWithoutExtension(_currentFilePath);

                    this.Title = $"Accès Client 1.4.3 / Partagé ({_originalFileName})";

                    var jsonData = File.ReadAllText(_currentFilePath);
                    var importedDatabase = JsonConvert.DeserializeObject<AccesClientWPF.Models.DatabaseModel>(jsonData);

                    if (importedDatabase != null)
                    {
                        _viewModel.LoadDatabase(importedDatabase);
                        if (_saveButton != null) _saveButton.IsEnabled = true;
                        if (_cancelButton != null) _cancelButton.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("Le fichier n'a pas pu être chargé ou est vide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_hasLock && !string.IsNullOrEmpty(_lockFilePath) && File.Exists(_lockFilePath))
            {
                try
                {
                    File.Delete(_lockFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression du verrou : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            base.OnClosed(e);
        }


        private void ManageClientsCommand_Click(object sender, RoutedEventArgs e)
        {

            var sharedClientWindow = new SharedClientManagementWindow(_viewModel);

            if (sharedClientWindow.ShowDialog() == true)
            {

                if (_viewModel.SelectedClient != null &&
                    !_viewModel.Clients.Contains(_viewModel.SelectedClient))
                {
                    _viewModel.SelectedClient = null;
                }
            }
        }

        private void CreateNewDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Demander confirmation avant de réinitialiser
            if (_saveButton != null && _saveButton.IsEnabled)
            {
                var result = MessageBox.Show("Voulez-vous enregistrer la base actuelle avant d'en créer une nouvelle ?",
                    "Confirmation", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                    SaveSharedDatabase_Click(null, null);
            }

            // Réinitialiser
            _viewModel.CreateNewDatabase();
            _currentFilePath = null;
            _originalFileName = null;
            this.Title = "Accès Client 1.3.0 / Partagé (Nouveau)";

            // Activer les boutons
            if (_saveButton != null) _saveButton.IsEnabled = true;
            if (_cancelButton != null) _cancelButton.IsEnabled = true;
        }

        // Corrigeons la fonction d'importation vers database.json
        private void ImportToMainDatabase_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier d'abord si une base est chargée
            if (string.IsNullOrEmpty(_currentFilePath) && _viewModel.Clients.Count == 0)
            {
                MessageBox.Show("Veuillez d'abord ouvrir ou créer une base de données.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Créer l'objet de base de données à partir du viewModel actuel
                var importedDatabase = new Models.DatabaseModel
                {
                    Clients = _viewModel.Clients,
                    Files = new ObservableCollection<FileModel>(_viewModel.AllFiles)
                };

                // Charger la base principale
                string mainDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.json");
                Models.DatabaseModel currentDatabase;

                if (File.Exists(mainDbPath))
                {
                    string jsonData = File.ReadAllText(mainDbPath);
                    currentDatabase = JsonConvert.DeserializeObject<Models.DatabaseModel>(jsonData)
                        ?? new Models.DatabaseModel();
                }
                else
                {
                    currentDatabase = new Models.DatabaseModel
                    {
                        Clients = new ObservableCollection<ClientModel>(),
                        Files = new ObservableCollection<FileModel>()
                    };
                }

                // Fusionner les clients
                foreach (var importedClient in importedDatabase.Clients)
                {
                    var existingClient = currentDatabase.Clients
                        .FirstOrDefault(c => c.Name == importedClient.Name);

                    if (existingClient == null)
                    {
                        currentDatabase.Clients.Add(importedClient);
                    }
                }

                // Fusionner les fichiers
                foreach (var importedFile in importedDatabase.Files)
                {
                    var existingFile = currentDatabase.Files
                        .FirstOrDefault(f => f.Name == importedFile.Name && f.Client == importedFile.Client);

                    if (existingFile == null)
                    {
                        currentDatabase.Files.Add(importedFile);
                    }
                    else
                    {
                        // Mise à jour de toutes les propriétés
                        existingFile.Type = importedFile.Type;
                        existingFile.FullPath = importedFile.FullPath;
                        existingFile.CustomIconPath = importedFile.CustomIconPath;
                        existingFile.WindowsUsername = importedFile.WindowsUsername;
                        existingFile.WindowsPassword = importedFile.WindowsPassword;
                    }
                }

                // Sauvegarder
                File.WriteAllText(mainDbPath, JsonConvert.SerializeObject(currentDatabase, Formatting.Indented));
                MessageBox.Show("Import réussi dans la base principale !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'importation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void SaveSharedDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsSharedDatabase();
                return;
            }

            try
            {
                // Créer l'objet de base de données à partir du viewModel
                var database = new Models.DatabaseModel
                {
                    Clients = _viewModel.Clients,
                    Files = new ObservableCollection<FileModel>(_viewModel.AllFiles)
                };

                // Sauvegarder au même emplacement
                File.WriteAllText(_currentFilePath, JsonConvert.SerializeObject(database, Formatting.Indented));
                MessageBox.Show("Base partagée sauvegardée avec succès !", "Sauvegarde réussie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsSharedDatabase()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Sauvegarder la base partagée",
                Filter = "Fichiers base partagée (*.antclient)|*.antclient|Fichiers JSON (*.json)|*.json",
                DefaultExt = ".antclient"
            };

            if (_originalFileName != null)
            {
                saveFileDialog.FileName = _originalFileName;
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                _originalFileName = Path.GetFileNameWithoutExtension(_currentFilePath);
                this.Title = $"Accès Client 1.1.0 ({_originalFileName})";
                SaveSharedDatabase_Click(null, null);
            }
        }

        private void CancelAndClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is FileModel selectedFile)
            {
                // Afficher les détails du fichier plutôt que de l'exécuter
                _viewModel.EditSelectedFile(selectedFile);
            }
        }

        private void AddContextMenu_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier uniquement si un client est sélectionné, pas si un élément est sélectionné
            if (_viewModel.SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Si un client est sélectionné, on peut ajouter un fichier
            _viewModel.AddFile();
        }

        private void EditContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedFile == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à modifier.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _viewModel.EditSelectedFile();
        }

        private void DeleteContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedFile != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'élément '{_viewModel.SelectedFile.Name}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteSelectedFile();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un élément à supprimer.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta / 3);
            e.Handled = true;
        }

        private void CopyWindowsUsername_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string username && !string.IsNullOrEmpty(username))
            {
                Helpers.ClipboardHelper.CopyPlainText(username);
                MessageBox.Show("Nom d'utilisateur Windows copié dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyWindowsPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileModel file)
            {
                if (!string.IsNullOrEmpty(file.WindowsPassword))
                {
                    string decryptedPassword = EncryptionHelper.Decrypt(file.WindowsPassword);
                    Helpers.ClipboardHelper.CopyPlainText(decryptedPassword);
                    MessageBox.Show("Mot de passe Windows copié dans le presse-papiers.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Méthode helper pour trouver un élément visuel enfant
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        private void TestConnectionMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedFile == null)
            {
                MessageBox.Show("Veuillez sélectionner un élément à tester.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var file = _viewModel.SelectedFile;

                switch (file.Type)
                {
                    case "RDS":
                        ConnectToRemoteDesktop(file);
                        break;
                    case "AnyDesk":
                        ConnectToAnyDesk(file);
                        break;
                    case "VPN":
                        LaunchProgram(file.FullPath);
                        break;
                    case "Dossier":
                        OpenFolder(file.FullPath);
                        break;
                    case "Fichier":
                        OpenFile(file.FullPath);
                        break;
                    default:
                        MessageBox.Show($"Test de connexion non implémenté pour le type '{file.Type}'.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du test de connexion : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchProgram(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start(path);
                }
                else
                {
                    MessageBox.Show($"Le fichier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du programme : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // Utiliser ProcessStartInfo pour une meilleure gestion
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = string.Format("\"{0}\"", path),
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Le dossier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToRemoteDesktop(FileModel file)
        {
            try
            {
                var credentials = file.FullPath.Split(':');
                if (credentials.Length < 3)
                {
                    MessageBox.Show("Les informations de connexion RDS sont incomplètes.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string ipDns = credentials[0];
                string username = credentials[1];
                string encryptedPassword = credentials[2];
                string password = EncryptionHelper.Decrypt(encryptedPassword);

                // Vérifier si le mot de passe est vide
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show($"Aucun mot de passe n'a été renseigné pour la connexion RDS '{file.Name}'.\nVeuillez éditer cette connexion et spécifier un mot de passe.",
                                   "Mot de passe manquant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Création du fichier RDP avec le titre personnalisé
                string rdpFilePath = Path.Combine(Path.GetTempPath(), $"{file.Name.Replace(' ', '_')}_{Guid.NewGuid().ToString().Substring(0, 8)}.rdp");

                using (StreamWriter sw = new StreamWriter(rdpFilePath))
                {
                    // Format RDP standard
                    sw.WriteLine("screen mode id:i:2");
                    sw.WriteLine($"full address:s:{ipDns}");
                    sw.WriteLine($"username:s:{username}");
                    sw.WriteLine("prompt for credentials:i:0");
                    sw.WriteLine("desktopwidth:i:0");
                    sw.WriteLine("desktopheight:i:0");
                    sw.WriteLine("session bpp:i:32");
                    sw.WriteLine($"use multimon:i:{(_viewModel.IsMultiMonitor ? "1" : "0")}");
                    sw.WriteLine("connection type:i:7");
                    sw.WriteLine("networkautodetect:i:1");
                    sw.WriteLine("bandwidthautodetect:i:1");
                    sw.WriteLine("authentication level:i:2");
                    sw.WriteLine("redirectsmartcards:i:1");
                    sw.WriteLine("redirectclipboard:i:1");
                    sw.WriteLine("audiomode:i:0");
                    sw.WriteLine("autoreconnection enabled:i:1");

                    // Paramètres pour définir le titre de la session
                    sw.WriteLine($"alternate shell:s:");
                    sw.WriteLine($"shell working directory:s:");
                    sw.WriteLine($"disable wallpaper:i:0");
                    sw.WriteLine($"allow font smoothing:i:1");
                    sw.WriteLine($"allow desktop composition:i:1");

                    // Définir le titre de la connexion - ce paramètre devrait fonctionner
                    sw.WriteLine($"title:s:{file.Name}");
                    sw.WriteLine($"promptcredentialonce:i:1");
                    sw.WriteLine($"winposstr:s:0,3,0,0,800,600");
                }

                // Stocker les identifiants temporairement
                try
                {
                    ProcessStartInfo cmdKeyInfo = new ProcessStartInfo
                    {
                        FileName = "cmdkey.exe",
                        Arguments = $"/generic:{ipDns} /user:{username} /pass:{password}",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (Process cmdKeyProcess = Process.Start(cmdKeyInfo))
                    {
                        cmdKeyProcess.WaitForExit();
                    }
                }
                catch
                {
                    // Ignorer les erreurs de cmdkey et continuer
                }

                // Lancer mstsc directement avec le nom de la connexion comme titre
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "mstsc.exe",
                    Arguments = $"\"{rdpFilePath}\" /f",
                    UseShellExecute = true
                };

                using (Process mstscProcess = Process.Start(startInfo))
                {
                    // Ne pas attendre la fin du processus
                }

                // Supprimer le fichier RDP après un délai
                Task.Delay(5000).ContinueWith(_ =>
                {
                    try
                    {
                        if (File.Exists(rdpFilePath))
                        {
                            File.Delete(rdpFilePath);
                        }

                        // Nettoyer les identifiants après un délai plus long
                        Task.Delay(30000).ContinueWith(__ =>
                        {
                            try
                            {
                                ProcessStartInfo cleanupInfo = new ProcessStartInfo
                                {
                                    FileName = "cmdkey.exe",
                                    Arguments = $"/delete:{ipDns}",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };

                                using (Process cleanupProcess = Process.Start(cleanupInfo))
                                {
                                    // Ne pas attendre
                                }
                            }
                            catch
                            {
                                // Ignorer les erreurs de nettoyage
                            }
                        });
                    }
                    catch
                    {
                        // Ignorer les erreurs de suppression
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la connexion RDS : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToAnyDesk(FileModel file)
        {
            try
            {
                var credentials = file.FullPath.Split(':');
                string id = credentials[0];
                string password = string.Empty;

                if (credentials.Length > 1 && !string.IsNullOrEmpty(credentials[1]))
                {
                    password = EncryptionHelper.Decrypt(credentials[1]);
                }

                // Vérifier le chemin d'AnyDesk
                if (!VerifyAndSetAnyDeskPath())
                {
                    return; // Si le chemin n'est pas valide après demande à l'utilisateur, on arrête
                }

                string anyDeskPath = AppSettings.Instance.AnyDeskPath;

                try
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // Exécution via CMD pour utiliser 'echo' avec le mot de passe
                        string command = $"echo {password} | \"{anyDeskPath}\" \"{id}\" --with-password";
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C {command}",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                    else
                    {
                        // Si pas de mot de passe, exécution simple
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = anyDeskPath,
                            Arguments = $"\"{id}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture de AnyDesk : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la préparation de la connexion AnyDesk : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Vérifier et demander le chemin d'AnyDesk si nécessaire
        private bool VerifyAndSetAnyDeskPath()
        {
            // Si le chemin par défaut est valide, pas besoin de demander
            if (AppSettings.Instance.IsAnyDeskPathValid())
            {
                return true;
            }

            // Sinon, demander à l'utilisateur
            MessageBox.Show(
                "Le chemin d'AnyDesk n'est pas valide ou n'a pas été configuré.\n" +
                "Veuillez sélectionner l'exécutable AnyDesk sur votre système.",
                "Configuration AnyDesk",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Sélectionner l'exécutable AnyDesk",
                Filter = "Exécutable (*.exe)|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedPath = openFileDialog.FileName;

                // Vérifier que le fichier sélectionné est bien AnyDesk
                if (Path.GetFileName(selectedPath).ToLower() == "anydesk.exe")
                {
                    // Sauvegarder le chemin
                    AppSettings.Instance.AnyDeskPath = selectedPath;
                    AppSettings.Instance.Save();
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "Le fichier sélectionné ne semble pas être AnyDesk.\n" +
                        "Veuillez sélectionner l'exécutable AnyDesk.exe.",
                        "Erreur de sélection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return VerifyAndSetAnyDeskPath(); // Nouvelle tentative
                }
            }

            return false; // L'utilisateur a annulé
        }

        private void MoveUpContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var ctx = mi?.Parent as ContextMenu;
            if (ctx?.PlacementTarget is FrameworkElement fe && fe.DataContext is FileModel item)
            {
                _viewModel.MovePasswordUpCommand.Execute(item);
            }
        }

        private void MoveDownContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var ctx = mi?.Parent as ContextMenu;
            if (ctx?.PlacementTarget is FrameworkElement fe && fe.DataContext is FileModel item)
            {
                _viewModel.MovePasswordDownCommand.Execute(item);
            }
        }


        private void FileList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 1) sélectionner l'item sous la souris
            var dep = (DependencyObject)e.OriginalSource;
            var lvi = FindParent<ListViewItem>(dep);
            if (lvi != null)
            {
                lvi.IsSelected = true;            // => met aussi SelectedItem
                FileList.Focus();
            }

            // 2) Récupérer le menu + activer/désactiver selon sélection
            ContextMenu menu = FileList.ContextMenu;
            if (menu == null) return;

            bool clientSelected = _viewModel.SelectedClient != null;
            bool itemSelected = FileList.SelectedItem != null;

            // Ordre attendu : 0 Ajouter, 1 Modifier, 2 Monter, 3 Descendre, 4 Tester, 5 Supprimer
            if (menu.Items.Count > 0) ((MenuItem)menu.Items[0]).IsEnabled = clientSelected; // Ajouter
            if (menu.Items.Count > 1) ((MenuItem)menu.Items[1]).IsEnabled = itemSelected;   // Modifier
            if (menu.Items.Count > 2) ((MenuItem)menu.Items[2]).IsEnabled = itemSelected;   // Monter
            if (menu.Items.Count > 3) ((MenuItem)menu.Items[3]).IsEnabled = itemSelected;   // Descendre
            if (menu.Items.Count > 4) ((MenuItem)menu.Items[4]).IsEnabled = itemSelected;   // Tester
            if (menu.Items.Count > 5) ((MenuItem)menu.Items[5]).IsEnabled = itemSelected;   // Supprimer

            // 3) ouvrir le menu à l’endroit du clic si un client est sélectionné
            if (clientSelected)
            {
                Point mousePosition = e.GetPosition(FileList);
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                menu.PlacementTarget = FileList;
                menu.HorizontalOffset = mousePosition.X;
                menu.VerticalOffset = mousePosition.Y;
                menu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void FileList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu menu = FileList.ContextMenu;
            if (menu == null) return;

            bool clientSelected = _viewModel.SelectedClient != null;
            bool itemSelected = FileList.SelectedItem != null;

            if (menu.Items.Count > 0) ((MenuItem)menu.Items[0]).IsEnabled = clientSelected; // Ajouter
            if (menu.Items.Count > 1) ((MenuItem)menu.Items[1]).IsEnabled = itemSelected;   // Modifier
            if (menu.Items.Count > 2) ((MenuItem)menu.Items[2]).IsEnabled = itemSelected;   // Monter
            if (menu.Items.Count > 3) ((MenuItem)menu.Items[3]).IsEnabled = itemSelected;   // Descendre
            if (menu.Items.Count > 4) ((MenuItem)menu.Items[4]).IsEnabled = itemSelected;   // Tester
            if (menu.Items.Count > 5) ((MenuItem)menu.Items[5]).IsEnabled = itemSelected;   // Supprimer

            if (!clientSelected) e.Handled = true;
        }



        private void AddButtonDirect_Click(object sender, RoutedEventArgs e)
        {
            // Appel direct à la méthode d'ajout (même logique que pour le menu contextuel)
            if (_viewModel.SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant d'ajouter un élément.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Si un client est sélectionné, on peut ajouter un fichier
            _viewModel.AddFile();
        }

        private void OpenFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show($"Le fichier '{path}' n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}