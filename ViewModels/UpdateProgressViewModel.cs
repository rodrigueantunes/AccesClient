using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AccesClientWPF.ViewModels
{
    public sealed class UpdateProgressViewModel : INotifyPropertyChanged
    {
        private string _title = "Mise à jour…";
        private string _status = "Initialisation…";
        private double _progress;
        private bool _canCancel = true;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public double ProgressPercent
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public bool CanCancel
        {
            get => _canCancel;
            set { _canCancel = value; OnPropertyChanged(); }
        }

        public event Action? CancelRequested;
        public void RaiseCancelRequested() => CancelRequested?.Invoke();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}