using System.Collections.ObjectModel;

namespace AccesClientWPF.Models
{
    public class DatabaseModel
    {
        public ObservableCollection<ClientModel> Clients { get; set; } = new();
        public ObservableCollection<FileModel> Files { get; set; } = new();
    }
}
