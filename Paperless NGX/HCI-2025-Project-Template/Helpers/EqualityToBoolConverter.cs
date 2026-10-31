using System;
using System.Globalization;
using System.Windows.Data;

namespace HCI_2025_Project_Template.Helpers
{
    /// <summary>Vraca true kada su dvije vrijednosti jednake — za oznacavanje aktivne stavke menija.</summary>
    public class EqualityToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            return string.Equals(values[0]?.ToString(), values[1]?.ToString(),
                StringComparison.Ordinal);
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}