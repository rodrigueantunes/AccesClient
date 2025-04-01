using System.Collections.ObjectModel;

namespace AccesClientWPF.Models
{
    public class DatabaseModel
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new ObservableCollection<ClientModel>();
        public ObservableCollection<FileModel> Files { get; set; } = new ObservableCollection<FileModel>();
    }
}