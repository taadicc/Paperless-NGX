using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HCI_2025_Project_Template.Helpers
{
    /// <summary>
    /// Dodjeljuje boju oznaci na osnovu njenog naziva.
    /// Dokumenti sa servera nose samo nazive oznaka, ne i njihove boje,
    /// pa boju izvodimo iz naziva. Isti naziv uvijek daje istu boju,
    /// tako da korisnik moze prepoznati oznaku po boji kroz cijelu aplikaciju.
    /// </summary>
    public class TagNameToColourConverter : IValueConverter
    {
        // Paleta biranа tako da susjedne boje budu jasno razlicite
        private static readonly string[] Palette =
        {
            "#EF4444", "#F97316", "#EAB308", "#84CC16", "#22C55E",
            "#10B981", "#0D9488", "#06B6D4", "#0EA5E9", "#3B82F6",
            "#6366F1", "#8B5CF6", "#A855F7", "#D946EF", "#EC4899",
            "#F43F5E", "#78716C", "#64748B"
        };

        internal static Color ColourFor(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (Color)ColorConverter.ConvertFromString("#64748B");

            // Stabilan hash - isti naziv daje istu boju pri svakom pokretanju
            unchecked
            {
                int hash = 17;
                foreach (char ch in name.Trim().ToLowerInvariant())
                    hash = hash * 31 + ch;

                int index = Math.Abs(hash) % Palette.Length;
                return (Color)ColorConverter.ConvertFromString(Palette[index]);
            }
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var brush = new SolidColorBrush(ColourFor(value as string));
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Bira citljivu boju teksta za boju koju daje TagNameToColourConverter.</summary>
    public class TagNameToContrastConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var c = TagNameToColourConverter.ColourFor(value as string);
            double L = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;

            var brush = new SolidColorBrush(L > 0.6
                ? Color.FromRgb(0x0F, 0x1B, 0x2D)
                : Colors.White);
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
