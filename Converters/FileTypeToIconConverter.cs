using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using AccesClientWPF.Models;

namespace AccesClientWPF.Converters
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is FileModel file)
                {
                    // Si une icône personnalisée est spécifiée et existe
                    if (!string.IsNullOrEmpty(file.CustomIconPath) && File.Exists(file.CustomIconPath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Important pour éviter les problèmes de verrouillage de fichier
                        bitmap.UriSource = new Uri(file.CustomIconPath);
                        bitmap.EndInit();
                        bitmap.Freeze(); // Améliore les performances
                        return bitmap;
                    }

                    // Sinon, utiliser l'icône par défaut basée sur le type
                    string iconPath = file.Type switch
                    {
                        "RDS" => "pack://application:,,,/AccesClientWPF;component/Resources/remote_desktop.png",
                        "VPN" => "pack://application:,,,/AccesClientWPF;component/Resources/vpn.png",
                        "AnyDesk" => "pack://application:,,,/AccesClientWPF;component/Resources/anydesk.png",
                        "Dossier" => "pack://application:,,,/AccesClientWPF;component/Resources/dossier.png",
                        "Fichier" => "pack://application:,,,/AccesClientWPF;component/Resources/fichier.png",
                        "Rangement" => "pack://application:,,,/AccesClientWPF;component/Resources/fleche.png",
                        _ => "pack://application:,,,/AccesClientWPF;component/Resources/default.png"
                    };
                    return new BitmapImage(new Uri(iconPath));
                }
                else if (value is string fileType)
                {
                    string iconPath = fileType switch
                    {
                        "RDS" => "pack://application:,,,/AccesClientWPF;component/Resources/remote_desktop.png",
                        "VPN" => "pack://application:,,,/AccesClientWPF;component/Resources/vpn.png",
                        "AnyDesk" => "pack://application:,,,/AccesClientWPF;component/Resources/anydesk.png",
                        "Dossier" => "pack://application:,,,/AccesClientWPF;component/Resources/dossier.png",
                        "Fichier" => "pack://application:,,,/AccesClientWPF;component/Resources/fichier.png",
                        "Rangement" => "pack://application:,,,/AccesClientWPF;component/Resources/fleche.png",
                        _ => "pack://application:,,,/AccesClientWPF;component/Resources/default.png"
                    };
                    return new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur de conversion d'icône: {ex.Message}");
                return new BitmapImage(new Uri("pack://application:,,,/AccesClientWPF;component/Resources/default.png", UriKind.Absolute));
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}