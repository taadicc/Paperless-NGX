using System;
using System.Globalization;
using System.Windows.Data;

namespace HCI_2025_Project_Template.Helpers
{
    /// <summary>
    /// Ono sto kontekstnom meniju treba: ViewModel prozora i id dokumenta.
    /// ContextMenu nije dio vizuelnog stabla prozora, pa se do ViewModela
    /// ne moze doci preko RelativeSource AncestorType=Window. Zato ga
    /// kartica spakuje u svoj Tag, a meni ga odatle procita.
    /// </summary>
    public class MenuContext
    {
        public object? Vm { get; init; }
        public int DocumentId { get; init; }
    }

    public class ContextMenuContextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return new MenuContext();

            int id = values[1] is int i ? i : 0;

            return new MenuContext
            {
                Vm = values[0],
                DocumentId = id
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
            object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}