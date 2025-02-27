namespace AccesClientWPF.Models
{
    public class RdsAccount
    {
        public string Description { get; set; }
        public string IpDns { get; set; }
        public string NomUtilisateur { get; set; }
        public string MotDePasse { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now; // ✅ Ajout ici
    }
}