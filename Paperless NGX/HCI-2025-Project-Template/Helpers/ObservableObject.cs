using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HCI_2025_Project_Template.Helpers
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged; // Dodaj upitnik ovdje

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) // I ovdje
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}