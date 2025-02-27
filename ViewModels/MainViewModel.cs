using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using AccesClientWPF.Models;
using Newtonsoft.Json;
using AccesClientWPF.Helpers;
using AccesClientWPF.Commands;
using System.Windows;
using AccesClientWPF.Views;

namespace AccesClientWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> AllFiles { get; set; } = new();
        private ClientModel _selectedClient;
        public ICommand ManageJsonCommand { get; }

        public ClientModel SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));
                if (value != null)
                    LoadFiles(value.Path);
            }
        }

        public MainViewModel()
        {
            LoadClients();
            ManageJsonCommand = new RelayCommand(OpenRdsAccountWindow);
        }

        private void LoadClients()
        {
            string directoryPath = @"C:\\Application\\Clients\\";
            if (Directory.Exists(directoryPath))
            {
                foreach (var dir in Directory.GetDirectories(directoryPath))
                {
                    Clients.Add(new ClientModel { Name = Path.GetFileName(dir), Path = dir });
                }
            }
        }

        private void LoadFiles(string directoryPath)
        {
            AllFiles.Clear();
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName.StartsWith("Any-")) fileName = fileName.Substring(4);
                else if (fileName.StartsWith("RDS-")) fileName = fileName.Substring(4);
                else if (fileName.StartsWith("VPN-")) fileName = fileName.Substring(4);

                string type = Path.GetFileName(file).StartsWith("Any-") ? "AnyDesk" :
                              Path.GetFileName(file).StartsWith("RDS-") ? "RDS" :
                              Path.GetFileName(file).StartsWith("VPN-") ? "VPN" : "Other";

                var fileModel = new FileModel { Name = fileName, Type = type, FullPath = file };
                AllFiles.Add(fileModel);
            }

            OnPropertyChanged(nameof(AllFiles));
        }

        public void HandleFileDoubleClick(FileModel file)
        {
            if (file == null) return;

            if (file.FullPath.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase))
            {
                ConnectToRemoteDesktop(file);
            }
            else
            {
                Process.Start(new ProcessStartInfo(file.FullPath) { UseShellExecute = true });
            }
        }

        private void ConnectToRemoteDesktop(FileModel file)
        {
            string jsonFilePath = @"C:\\Application\\Clients\\rds_accounts.json";
            if (File.Exists(jsonFilePath))
            {
                var jsonData = File.ReadAllText(jsonFilePath);
                var rdsAccounts = JsonConvert.DeserializeObject<ObservableCollection<RdsAccount>>(jsonData);
                var credentials = rdsAccounts?.FirstOrDefault(a => a.Description.Equals(file.Name, StringComparison.OrdinalIgnoreCase));

                if (credentials != null)
                {
                    string decryptedPassword = EncryptionHelper.Decrypt(credentials.MotDePasse);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C cmdkey /generic:{credentials.IpDns} /user:\"{credentials.NomUtilisateur}\" /pass:\"{decryptedPassword}\" && mstsc {file.FullPath}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else
                {
                    MessageBox.Show("Aucune information de connexion enregistrée pour ce fichier RDP.");
                }
            }
        }

        private void OpenRdsAccountWindow(object parameter)
        {
            var rdsAccountWindow = new RdsAccountWindow();
            if (rdsAccountWindow.ShowDialog() == true)
            {
                LoadFiles(SelectedClient?.Path);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
