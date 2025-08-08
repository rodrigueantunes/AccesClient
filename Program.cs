namespace Accès_client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialiser les configurations de l'application
            Application.EnableVisualStyles(); // Appliquer les styles visuels
            Application.SetCompatibleTextRenderingDefault(false); // Configurer le rendu de texte

            // Lancer Form1
            Application.Run(new Form1()); // Votre formulaire principal

            // ApplicationConfiguration.Initialize(); // Cette ligne peut être supprimée, elle semble ne pas être nécessaire
        }
    }
}
