using System.Configuration;
using System.Data;
using System.Windows;

namespace HCI_2025_Project_Template
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Services.ThemeService.LoadSaved();
            Services.LocalizationService.LoadSaved();
            base.OnStartup(e);
        }
    }

}
