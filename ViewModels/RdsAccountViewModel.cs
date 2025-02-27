using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using AccesClientWPF.Models;
using AccesClientWPF.Commands;
using AccesClientWPF.Views;
using System.ComponentModel;

namespace AccesClientWPF.ViewModels
{
    public class RdsAccountViewModel : INotifyPropertyChanged
    {
        private const string JsonFilePath = @"C:\Application\Clients\rds_accounts.json";
        public ObservableCollection<RdsAccount> RdsAccounts { get; set; } = new();
        private RdsAccount _selectedRdsAccount;

        public RdsAccount SelectedRdsAccount
        {
            get => _selectedRdsAccount;
            set
            {
                _selectedRdsAccount = value;
                OnPropertyChanged(nameof(SelectedRdsAccount));
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public RdsAccountViewModel()
        {
            LoadAccounts();
            AddCommand = new RelayCommand(_ => AddAccount());
            EditCommand = new RelayCommand(_ => EditAccount(), _ => SelectedRdsAccount != null);
            DeleteCommand = new RelayCommand(_ => DeleteAccount(), _ => SelectedRdsAccount != null);
            CloseCommand = new RelayCommand(_ => CloseWindow());
        }

        private void LoadAccounts()
        {
            if (File.Exists(JsonFilePath))
            {
                var jsonData = File.ReadAllText(JsonFilePath);
                RdsAccounts = JsonConvert.DeserializeObject<ObservableCollection<RdsAccount>>(jsonData) ?? new ObservableCollection<RdsAccount>();
                OnPropertyChanged(nameof(RdsAccounts));
            }
        }

        private void SaveAccounts()
        {
            File.WriteAllText(JsonFilePath, JsonConvert.SerializeObject(RdsAccounts, Formatting.Indented));
            OnPropertyChanged(nameof(RdsAccounts));
        }

        private void AddAccount()
        {
            var newAccount = new RdsAccount
            {
                Description = "",
                IpDns = "",
                NomUtilisateur = "",
                MotDePasse = "",
                DateCreation = DateTime.Now
            };

            var editWindow = new EditRdsAccountWindow(newAccount);
            if (editWindow.ShowDialog() == true)
            {
                RdsAccounts.Add(editWindow.RdsAccount);
                SaveAccounts();
            }
        }

        private void EditAccount()
        {
            if (SelectedRdsAccount != null)
            {
                var editWindow = new EditRdsAccountWindow(SelectedRdsAccount);
                if (editWindow.ShowDialog() == true)
                {
                    SelectedRdsAccount.Description = editWindow.RdsAccount.Description;
                    SelectedRdsAccount.IpDns = editWindow.RdsAccount.IpDns;
                    SelectedRdsAccount.NomUtilisateur = editWindow.RdsAccount.NomUtilisateur;
                    SelectedRdsAccount.MotDePasse = editWindow.RdsAccount.MotDePasse;
                    SaveAccounts();
                }
            }
        }

        private void DeleteAccount()
        {
            if (SelectedRdsAccount != null)
            {
                RdsAccounts.Remove(SelectedRdsAccount);
                SaveAccounts();
            }
        }

        private void CloseWindow()
        {
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is RdsAccountWindow)?.Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
