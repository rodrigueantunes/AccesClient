using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AccesClientWPF.Models
{
    public sealed class MonitorSelectionSlot : INotifyPropertyChanged
    {
        public int SlotNumber { get; }

        private ObservableCollection<ScreenItem> _availableScreens = new();
        public ObservableCollection<ScreenItem> AvailableScreens
        {
            get => _availableScreens;
            set
            {
                if (!ReferenceEquals(_availableScreens, value))
                {
                    _availableScreens = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableScreens)));
                }
            }
        }

        private ScreenItem _selectedScreen;
        public ScreenItem SelectedScreen
        {
            get => _selectedScreen;
            set
            {
                if (!Equals(_selectedScreen, value))
                {
                    _selectedScreen = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedScreen)));
                    _onChanged?.Invoke(this);
                }
            }
        }

        private readonly Action<MonitorSelectionSlot> _onChanged;

        public MonitorSelectionSlot(int slotNumber, Action<MonitorSelectionSlot> onChanged)
        {
            SlotNumber = slotNumber;
            _onChanged = onChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
