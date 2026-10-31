using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HCI_2025_Project_Template.Helpers
{
    /// <summary>
    /// Pretvara hex string ("#1F78B4") u cetkicu.
    /// Bez ovoga se boje oznaka ne mogu vezati direktno u XAML-u,
    /// pa su svi uzorci ispadali iste boje.
    /// </summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex))
                return Brushes.Gray;

            try
            {
                if (!hex.StartsWith("#")) hex = "#" + hex;
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Vraca citljivu boju teksta (tamnu ili svijetlu) za datu pozadinu.
    /// Koristi se da naziv oznake ostane citljiv bez obzira na boju oznake.
    /// </summary>
    public class HexToContrastBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush DarkText =
            new(Color.FromRgb(0x0F, 0x1B, 0x2D));
        private static readonly SolidColorBrush LightText =
            new(Color.FromRgb(0xFF, 0xFF, 0xFF));

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var hex = value as string;
            if (string.IsNullOrWhiteSpace(hex)) return DarkText;

            try
            {
                if (!hex.StartsWith("#")) hex = "#" + hex;
                var c = (Color)ColorConverter.ConvertFromString(hex);

                // Relativna luminancija po WCAG preporuci
                double L = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
                return L > 0.6 ? DarkText : LightText;
            }
            catch
            {
                return DarkText;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
