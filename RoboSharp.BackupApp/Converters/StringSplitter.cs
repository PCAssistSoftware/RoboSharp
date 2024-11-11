using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RoboSharp.BackupApp.Converters
{
    [ValueConversion(typeof(string[]), typeof(string))]
    internal class StringSplitter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> arr)
                return string.Join(" ", arr);
            return string.Empty; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Split(' ');
            return Array.Empty<string>();
        }
    }
}
