using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using HCI_2025_Project_Template.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HCI_2025_Project_Template.ViewModels
{
    public class DocumentDetailViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;

        private DocumentDetail _document;
        private bool _isBusy;
        private string _activeTab = "detalji";

        public Action? GoBackRequested { get; set; }

        // ── Properties ─────────────────────────────────────────

        public DocumentDetail Document
        {
            get => _document;
            set { _document = value; OnPropertyChanged(); }
        }

        

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }


        public string ActiveTab
        {
            get => _activeTab;
            set { _activeTab = value; OnPropertyChanged(); }
        }

        public string AddedFormatted =>
            _document?.Added == default ? "—"
            : _document.Added.ToString("dd.MM.yyyy HH:mm");

        public string ModifiedFormatted =>
            _document?.Modified == default ? "—"
            : _document.Modified.ToString("dd.MM.yyyy HH:mm");

        public ObservableCollection<AuditLogEntry> History { get; } = new();

        // ── Commands ───────────────────────────────────────────

        public RelayCommand GoBackCommand { get; }
        public RelayCommand DownloadCommand { get; }
        public RelayCommand TabDetailsCommand { get; }
        public RelayCommand TabContentCommand { get; }
        public RelayCommand TabHistoryCommand { get; }

        // ── Constructor ───────────────────────────────────────

        public DocumentDetailViewModel(DocumentDetail document)
        {
            _documentService = new DocumentService();
            _document = document ?? throw new ArgumentNullException(nameof(document));


            GoBackCommand = new RelayCommand(_ => GoBackRequested?.Invoke());

            
            DownloadCommand = new RelayCommand(async _ => await SaveAsAsync());
            TabDetailsCommand = new RelayCommand(_ => ActiveTab = "detalji");
            TabContentCommand = new RelayCommand(_ => ActiveTab = "sadrzaj");
            TabHistoryCommand = new RelayCommand(async _ => await OpenHistoryAsync());

            _ = InitAsync();
        }

        // ── Init ──────────────────────────────────────────────

        private async Task SaveAsAsync()
        {
            var safe = string.Join("_", (_document.Title ?? $"dokument_{_document.Id}")
                .Split(System.IO.Path.GetInvalidFileNameChars()));

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{safe}.pdf",
                Filter = "PDF dokument (*.pdf)|*.pdf|Svi fajlovi (*.*)|*.*",
                Title = "Sačuvaj dokument"
            };

            if (dialog.ShowDialog() != true) return;

            IsBusy = true;
            try
            {
                var ok = await _documentService.SaveDocumentAsync(_document.Id, dialog.FileName);
                if (ok)
                    Notifier.Success("Dokument je sačuvan",
                        System.IO.Path.GetFileName(dialog.FileName));
            }
            finally { IsBusy = false; }
        }
        private async Task InitAsync()
        {
            try
            {
                if (_document.Thumbnail == null && !string.IsNullOrEmpty(_document.ThumbnailUrl))
                {
                    var bmp = await AuthenticatedImageHelper.LoadImageAsync(_document.ThumbnailUrl);

                    if (bmp != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _document.Thumbnail = bmp;
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Init error: {ex.Message}");
            }
        }

        // ── Page Navigation ───────────────────────────────────

        

        // ── Load Page ─────────────────────────────────────────

        

        // ── History ───────────────────────────────────────────

        private async Task OpenHistoryAsync()
        {
            ActiveTab = "historija";

            if (History.Count == 0)
                await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var client = ApiService.GetClient();
                var r = await client.GetAsync($"documents/{_document.Id}/history/");

                if (!r.IsSuccessStatusCode)
                    return;

                var stream = await r.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var entries = await JsonSerializer.DeserializeAsync<List<AuditLogEntry>>(stream, options);

                if (entries == null)
                    return;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    History.Clear();
                    foreach (var e in entries)
                        History.Add(e);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }
    }
}