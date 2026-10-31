using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using HCI_2025_Project_Template.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HCI_2025_Project_Template.ViewModels
{
    public class UploadViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        private readonly Dictionary<int, Tag> _tagMap;
        private readonly Dictionary<int, DocumentType> _docTypeMap;
        private readonly Dictionary<int, Correspondent> _corrMap;

        // ── File ──────────────────────────────────────────────────────────
        private string _filePath = string.Empty;
        private string _fileName = string.Empty;
        private bool _hasFile;

        // ── Form fields ───────────────────────────────────────────────────
        private string _title = string.Empty;
        private int? _selectedDocumentTypeId;
        private int? _selectedCorrespondentId;
        private DateTime _created = DateTime.Today;

        // ── State ─────────────────────────────────────────────────────────
        private bool _isBusy;
        private bool _isSuccess;
        private string _statusMessage = string.Empty;
        private string _validationError = string.Empty;

        // ── Tag selection ─────────────────────────────────────────────────
        public ObservableCollection<SelectableTag> SelectableTags { get; } = new();

        // ── Lookup sources ────────────────────────────────────────────────
        public ObservableCollection<DocumentType> DocumentTypes { get; } = new();
        public ObservableCollection<Correspondent> Correspondents { get; } = new();

        // ── Callback: upload done ─────────────────────────────────────────
        public Action? UploadCompleted { get; set; }
        public Action? GoBackRequested { get; set; }

        // ── Properties ────────────────────────────────────────────────────

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public bool HasFile
        {
            get => _hasFile;
            set { _hasFile = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); ValidationError = ""; }
        }

        public int? SelectedDocumentTypeId
        {
            get => _selectedDocumentTypeId;
            set { _selectedDocumentTypeId = value; OnPropertyChanged(); }
        }

        public int? SelectedCorrespondentId
        {
            get => _selectedCorrespondentId;
            set { _selectedCorrespondentId = value; OnPropertyChanged(); }
        }

        public DateTime Created
        {
            get => _created;
            set { _created = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set { _isSuccess = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string ValidationError
        {
            get => _validationError;
            set { _validationError = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────
        public RelayCommand BrowseFileCommand { get; }
        public RelayCommand UploadCommand { get; }
        public RelayCommand GoBackCommand { get; }
        public RelayCommand ClearCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────

        public UploadViewModel(
            DocumentService documentService,
            Dictionary<int, Tag> tagMap,
            Dictionary<int, DocumentType> docTypeMap,
            Dictionary<int, Correspondent> corrMap)
        {
            _documentService = documentService;
            _tagMap = tagMap;
            _docTypeMap = docTypeMap;
            _corrMap = corrMap;

            // Populate dropdowns
            foreach (var dt in docTypeMap.Values.OrderBy(d => d.Name))
                DocumentTypes.Add(dt);
            foreach (var c in corrMap.Values.OrderBy(c => c.Name))
                Correspondents.Add(c);
            foreach (var t in tagMap.Values.OrderBy(t => t.Name))
                SelectableTags.Add(new SelectableTag(t));

            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            UploadCommand = new RelayCommand(async _ => await UploadAsync(), _ => HasFile && !IsBusy);
            GoBackCommand = new RelayCommand(_ => GoBackRequested?.Invoke());
            ClearCommand = new RelayCommand(_ => ClearForm());
        }

        // ── File picker ───────────────────────────────────────────────────

        private void BrowseFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Odaberi dokument za upload",
                Filter = "Dokumenti|*.pdf;*.png;*.jpg;*.jpeg;*.tiff;*.tif;*.gif;*.webp|" +
                         "PDF fajlovi|*.pdf|" +
                         "Slike|*.png;*.jpg;*.jpeg;*.tiff;*.tif;*.gif;*.webp|" +
                         "Svi fajlovi|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                FilePath = dlg.FileName;
                FileName = Path.GetFileName(dlg.FileName);
                HasFile = true;
                // Predloži naslov iz naziva fajla
                if (string.IsNullOrWhiteSpace(Title))
                    Title = Path.GetFileNameWithoutExtension(dlg.FileName);
                ValidationError = "";
                StatusMessage = "";
                IsSuccess = false;
            }
        }

        // ── Upload ────────────────────────────────────────────────────────

        private async Task UploadAsync()
        {
            // Validacija
            if (string.IsNullOrWhiteSpace(Title))
            {
                ValidationError = "Naslov dokumenta je obavezan.";
                return;
            }
            if (!HasFile || !File.Exists(FilePath))
            {
                ValidationError = "Odaberi fajl za upload.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Uploading...";
            IsSuccess = false;
            ValidationError = "";

            try
            {
                var client = ApiService.GetClient();

                using var form = new MultipartFormDataContent();

                // Fajl
                var fileBytes = await File.ReadAllBytesAsync(FilePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        GetMimeType(FilePath));
                form.Add(fileContent, "document", FileName);

                // Metapodaci
                form.Add(new StringContent(Title.Trim()), "title");
                form.Add(new StringContent(Created.ToString("yyyy-MM-dd")), "created");

                if (SelectedDocumentTypeId.HasValue)
                    form.Add(new StringContent(SelectedDocumentTypeId.Value.ToString()),
                        "document_type");

                if (SelectedCorrespondentId.HasValue)
                    form.Add(new StringContent(SelectedCorrespondentId.Value.ToString()),
                        "correspondent");

                // Tagovi (svaki kao poseban entry)
                var selectedTagIds = SelectableTags
                    .Where(t => t.IsSelected)
                    .Select(t => t.Tag.Id)
                    .ToList();

                foreach (var tagId in selectedTagIds)
                    form.Add(new StringContent(tagId.ToString()), "tags");

                var response = await client.PostAsync("documents/post_document/", form);

                if (response.IsSuccessStatusCode)
                {
                    IsSuccess = true;
                    StatusMessage = "Dokument je uspješno uploadovan!";
                    UploadCompleted?.Invoke();
                    // Počisti formu
                    ClearForm();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    StatusMessage = $"Greška: {response.StatusCode}";
                    ValidationError = ParseApiError(err);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "";
                ValidationError = $"Greška pri uploadu: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearForm()
        {
            FilePath = string.Empty;
            FileName = string.Empty;
            HasFile = false;
            Title = string.Empty;
            SelectedDocumentTypeId = null;
            SelectedCorrespondentId = null;
            Created = DateTime.Today;
            ValidationError = string.Empty;
            foreach (var t in SelectableTags)
                t.IsSelected = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".tiff" => "image/tiff",
                ".tif" => "image/tiff",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        private static string ParseApiError(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                var sb = new System.Text.StringBuilder();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                        sb.AppendLine($"{prop.Name}: {string.Join(", ", prop.Value.EnumerateArray().Select(e => e.GetString()))}");
                    else
                        sb.AppendLine($"{prop.Name}: {prop.Value}");
                }
                return sb.ToString().Trim();
            }
            catch
            {
                return json.Length > 200 ? json[..200] : json;
            }
        }
    }

    /// <summary>Wrapper za tag sa IsSelected flagom.</summary>
    public class SelectableTag : ObservableObject
    {
        private bool _isSelected;

        public Tag Tag { get; }
        public string Name => Tag.Name;
        public string DisplayColor => Tag.DisplayColor;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public SelectableTag(Tag tag) => Tag = tag;
    }
}