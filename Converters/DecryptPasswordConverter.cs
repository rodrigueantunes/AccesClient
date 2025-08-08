using System;
using System.Globalization;
using System.Windows.Data;
using AccesClientWPF.Helpers;

namespace AccesClientWPF.Converters
{
    public class DecryptPasswordConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                var dec = EncryptionHelper.Decrypt(s);
                return string.IsNullOrEmpty(dec) ? s : dec; // fallback en clair si échec
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
