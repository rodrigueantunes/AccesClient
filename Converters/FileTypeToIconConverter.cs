using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Converters
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileType)
            {
                string iconPath = fileType switch
                {
                    "RDS" => "pack://application:,,,/AccesClientWPF;component/Resources/remote_desktop.png",
                    "VPN" => "pack://application:,,,/AccesClientWPF;component/Resources/vpn.png",
                    "AnyDesk" => "pack://application:,,,/AccesClientWPF;component/Resources/anydesk.png",
                    _ => "pack://application:,,,/AccesClientWPF;component/Resources/default.png"
                };

                try
                {
                    return new BitmapImage(new Uri(iconPath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du chargement de l'icône : {ex.Message}");
                    return new BitmapImage(new Uri("pack://application:,,,/AccesClientWPF;component/Resources/default.png"));
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
