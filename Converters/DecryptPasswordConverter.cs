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
            if (value is string encryptedPassword && !string.IsNullOrEmpty(encryptedPassword))
            {
                return EncryptionHelper.Decrypt(encryptedPassword);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}