using HCI_2025_Project_Template.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace HCI_2025_Project_Template.Views
{
    public partial class PdfPreviewView : UserControl
    {
        private readonly DocumentService _service = new();
        private bool _isReady;

        public static readonly DependencyProperty DocumentIdProperty =
            DependencyProperty.Register(nameof(DocumentId), typeof(int),
                typeof(PdfPreviewView),
                new PropertyMetadata(0, OnDocumentChanged));

        public static readonly DependencyProperty DocumentTitleProperty =
            DependencyProperty.Register(nameof(DocumentTitle), typeof(string),
                typeof(PdfPreviewView), new PropertyMetadata(string.Empty));

        public int DocumentId
        {
            get => (int)GetValue(DocumentIdProperty);
            set => SetValue(DocumentIdProperty, value);
        }

        public string DocumentTitle
        {
            get => (string)GetValue(DocumentTitleProperty);
            set => SetValue(DocumentTitleProperty, value);
        }

        public PdfPreviewView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isReady) return;

            try
            {
                await Browser.EnsureCoreWebView2Async();

                // Sakrij Edge kontekstni meni i traku za preuzimanje —
                // korisnik ostaje u nasoj aplikaciji
                Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;

                _isReady = true;

                if (DocumentId > 0)
                    await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowError("Pregled nije podržan na ovom računaru");
                System.Diagnostics.Debug.WriteLine($"WebView2 init: {ex.Message}");
            }
        }

        private static async void OnDocumentChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PdfPreviewView view && view._isReady)
                await view.LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {

            if (DocumentId <= 0)
            {
                ShowError("Nijedan dokument nije izabran");
                return;
            }
            ShowLoading();

            var path = await _service.DownloadPreviewToTempAsync(DocumentId, DocumentTitle);

            if (path == null)
            {
                ShowError("Pregled nije dostupan");
                return;
            }

            // #toolbar=1 zadrzava Edge-ovu traku sa zumom i pretragom
            Browser.CoreWebView2.Navigate(new Uri(path).AbsoluteUri + "#toolbar=1");

            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            Browser.Visibility = Visibility.Visible;
        }

        private void ShowLoading()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            Browser.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            LoadingPanel.Visibility = Visibility.Collapsed;
            Browser.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
        }
    }
}