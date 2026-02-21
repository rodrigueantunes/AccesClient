using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AccesClientWPF.Converters
{
    /// <summary>
    /// Extrait une partie de FullPath séparé par ':'.
    /// - RDS : ip:user:encryptedPass => 0/1/2
    /// - AnyDesk : id:encryptedPass => 0/1
    /// </summary>
    public sealed class FullPathPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string ?? string.Empty;

            if (parameter == null) return string.Empty;
            if (!int.TryParse(parameter.ToString(), out int idx)) return string.Empty;

            var parts = s.Split(new[] { ':' }, StringSplitOptions.None);
            if (idx < 0 || idx >= parts.Length) return string.Empty;

            return parts[idx] ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}