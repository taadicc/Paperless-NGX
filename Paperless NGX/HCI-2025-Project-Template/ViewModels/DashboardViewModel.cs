using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using HCI_2025_Project_Template.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HCI_2025_Project_Template.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        private readonly Dictionary<int, Tag> _tagMap;
        private readonly Dictionary<int, DocumentType> _docTypeMap;
        private readonly Dictionary<int, Correspondent> _corrMap;

        private Statistics _stats = new();
        private bool _isBusy;

        // ── Callback prema MainViewModel-u ────────────────────────────────
        // Kad korisnik klikne na dokument na dashboardu, pozivamo ovo
        public Action<int>? OnDocumentSelected { get; set; }
        public Action<int>? OnDocumentOpened { get; set; }
        public RelayCommand OpenDetailCommand { get; }
        public ObservableCollection<Document> RecentDocuments { get; } = new();

        public Statistics Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        // Command koji karte pozivaju
        public RelayCommand SelectDocumentCommand { get; }

        public DashboardViewModel(
            DocumentService documentService,
            Dictionary<int, Tag> tagMap,
            Dictionary<int, DocumentType> docTypeMap,
            Dictionary<int, Correspondent> corrMap)
        {
            _documentService = documentService;
            _tagMap = tagMap;
            _docTypeMap = docTypeMap;
            _corrMap = corrMap;

            SelectDocumentCommand = new RelayCommand(param =>
            {
                if (param is int id)
                    OnDocumentSelected?.Invoke(id);
            });

            _ = LoadAsync();

            OpenDetailCommand = new RelayCommand(param =>
            {
                if (param is int id)
                    OnDocumentOpened?.Invoke(id);
            });
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var statsTask = _documentService.GetStatisticsAsync();
                var docsTask = _documentService.GetDocumentsAsync(page: 1);
                await Task.WhenAll(statsTask, docsTask);

                Stats = statsTask.Result;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentDocuments.Clear();
                    foreach (var doc in docsTask.Result.Results.Take(10))
                    {
                        doc.DocumentTypeName = doc.DocumentTypeId.HasValue
                            && _docTypeMap.TryGetValue(doc.DocumentTypeId.Value, out var dt)
                            ? dt.Name : "—";

                        doc.CorrespondentName = doc.CorrespondentId.HasValue
                            && _corrMap.TryGetValue(doc.CorrespondentId.Value, out var co)
                            ? co.Name : null;

                        doc.TagNames = doc.TagIds
                            .Select(id => _tagMap.TryGetValue(id, out var t)
                                ? t.Name : $"#{id}")
                            .ToList();

                        RecentDocuments.Add(doc);
                    }
                });

                foreach (var doc in RecentDocuments.ToList())
                    _ = LoadThumbAsync(doc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task LoadThumbAsync(Document doc)
        {
            try
            {
                var bmp = await AuthenticatedImageHelper.LoadImageAsync(doc.ThumbnailUrl);
                if (bmp != null)
                    Application.Current.Dispatcher.Invoke(() => doc.Thumbnail = bmp);
            }
            catch { }
        }
    }
}