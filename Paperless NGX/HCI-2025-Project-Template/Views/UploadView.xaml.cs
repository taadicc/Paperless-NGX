using HCI_2025_Project_Template.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace HCI_2025_Project_Template.Views
{
    public partial class UploadView : UserControl
    {
        public UploadView()
        {
            InitializeComponent();

            // Drag & drop podrška
            AllowDrop = true;
            DragOver += OnDragOver;
            Drop += OnDrop;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && DataContext is UploadViewModel vm)
            {
                var path = files[0];
                vm.FilePath = path;
                vm.FileName = Path.GetFileName(path);
                vm.HasFile = true;
                if (string.IsNullOrWhiteSpace(vm.Title))
                    vm.Title = Path.GetFileNameWithoutExtension(path);
            }
        }
    }
}