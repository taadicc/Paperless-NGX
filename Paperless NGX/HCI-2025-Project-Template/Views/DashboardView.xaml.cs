using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HCI_2025_Project_Template.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void DocumentCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe
                && fe.DataContext is Models.Document doc
                && DataContext is ViewModels.DashboardViewModel vm)
            {
                vm.SelectDocumentCommand.Execute(doc.Id);
            }
        }

        private void DocumentCard_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe
                && fe.DataContext is Models.Document doc
                && DataContext is ViewModels.DashboardViewModel vm)
            {
                vm.OpenDetailCommand.Execute(doc.Id);
                e.Handled = true;
            }
        }
    }
}
