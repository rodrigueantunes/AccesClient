namespace AccesClientWPF.Services
{
    public class VpnService
    {
        public async Task<bool> ConnectToFortiClientVPN(string vpn, string ip, string user, string password)
        {
            // Simulation de connexion VPN
            return await Task.FromResult(true);
        }
    }
}