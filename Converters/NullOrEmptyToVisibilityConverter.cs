using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AccesClientWPF.Converters
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter != null && parameter.ToString().ToLower() == "invert";

            if (value is string stringValue)
            {
                bool isEmpty = string.IsNullOrEmpty(stringValue);
                return (isEmpty ^ invert) ? Visibility.Collapsed : Visibility.Visible;
            }

            return invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}