using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using HCI_2025_Project_Template.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HCI_2025_Project_Template.ViewModels
{
    public class SearchViewModel : ObservableObject
    {
        private readonly SearchService _searchService;
        private readonly Dictionary<int, Models.Tag> _tagMap;
        private readonly Dictionary<int, Models.DocumentType> _docTypeMap;
        private readonly Dictionary<int, Models.Correspondent> _corrMap;

        private string _query = string.Empty;
        private bool _isBusy;
        private bool _hasResults;
        private bool _hasSearched;
        private int _totalResults;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // Autocomplete
        private bool _showSuggestions;
        private CancellationTokenSource? _autocompleteCts;

        // Callback — kad korisnik klikne na rezultat
        public Action<int>? OnDocumentSelected { get; set; }

        public ObservableCollection<SearchResult> Results { get; } = new();
        public ObservableCollection<string> Suggestions { get; } = new();

        // ── Properties ────────────────────────────────────────────────────

        public string Query
        {
            get => _query;
            set
            {
                _query = value;
                OnPropertyChanged();
                _ = TriggerAutocompleteAsync(value);
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasResults
        {
            get => _hasResults;
            set { _hasResults = value; OnPropertyChanged(); }
        }

        public bool HasSearched
        {
            get => _hasSearched;
            set { _hasSearched = value; OnPropertyChanged(); }
        }

        public int TotalResults
        {
            get => _totalResults;
            set { _totalResults = value; OnPropertyChanged(); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); }
        }

        public bool ShowSuggestions
        {
            get => _showSuggestions;
            set { _showSuggestions = value; OnPropertyChanged(); }
        }

        // ── Commands ──────────────────────────────────────────────────────

        public RelayCommand SearchCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PrevPageCommand { get; }
        public RelayCommand<string> UseSuggestionCommand { get; }
        public RelayCommand SelectResultCommand { get; }
        //public RelayCommand GoBackCommand { get; }
        public Action<int>? OnDocumentOpened { get; set; }
        public RelayCommand OpenResultCommand { get; }
        public Action? GoBackRequested { get; set; }

        // ── Constructor ───────────────────────────────────────────────────

        public SearchViewModel(
            Dictionary<int, Models.Tag> tagMap,
            Dictionary<int, Models.DocumentType> docTypeMap,
            Dictionary<int, Models.Correspondent> corrMap,
            string? initialQuery = null)
        {
            _searchService = new SearchService();
            _tagMap = tagMap;
            _docTypeMap = docTypeMap;
            _corrMap = corrMap;

            SearchCommand = new RelayCommand(async _ =>
            {
                ShowSuggestions = false;
                CurrentPage = 1;
                await ExecuteSearchAsync();
            });

            OpenResultCommand = new RelayCommand(param =>
            {
                if (param is int id)
                    OnDocumentOpened?.Invoke(id);
            });

            ClearCommand = new RelayCommand(_ =>
            {
                Query = string.Empty;
                Results.Clear();
                Suggestions.Clear();
                HasSearched = false;
                HasResults = false;
                ShowSuggestions = false;
            });

            NextPageCommand = new RelayCommand(async _ =>
            {
                if (CurrentPage < TotalPages)
                {
                    CurrentPage++;
                    await ExecuteSearchAsync();
                }
            });

            PrevPageCommand = new RelayCommand(async _ =>
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                    await ExecuteSearchAsync();
                }
            });

            UseSuggestionCommand = new RelayCommand<string>(async suggestion =>
            {
                if (string.IsNullOrEmpty(suggestion)) return;
                Query = suggestion;
                ShowSuggestions = false;
                CurrentPage = 1;
                await ExecuteSearchAsync();
            });

            SelectResultCommand = new RelayCommand(param =>
            {
                if (param is int id)
                    OnDocumentSelected?.Invoke(id);
            });

            //GoBackCommand = new RelayCommand(_ => GoBackRequested?.Invoke());

            // Ako imamo initial query (npr. iz header search boxa)
            if (!string.IsNullOrEmpty(initialQuery))
            {
                _query = initialQuery;
                OnPropertyChanged(nameof(Query));
                _ = ExecuteSearchAsync();
            }
        }

        public void UpdateQueryAndSearch(string query)
        {
            if (_query == query) return;
            _query = query;
            OnPropertyChanged(nameof(Query));
            CurrentPage = 1;
            _ = ExecuteSearchAsync();
        }

        // ── Search ────────────────────────────────────────────────────────

        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(Query)) return;

            IsBusy = true;
            try
            {
                var response = await _searchService.SearchAsync(Query, CurrentPage);

                TotalResults = response.Count;
                TotalPages = (int)Math.Ceiling(response.Count / 25.0);
                if (TotalPages < 1) TotalPages = 1;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Results.Clear();
                    foreach (var r in response.Results)
                    {
                        // Resolve names
                        r.DocumentTypeName = r.DocumentTypeId.HasValue
                            && _docTypeMap.TryGetValue(r.DocumentTypeId.Value, out var dt)
                            ? dt.Name : "—";

                        r.CorrespondentName = r.CorrespondentId.HasValue
                            && _corrMap.TryGetValue(r.CorrespondentId.Value, out var co)
                            ? co.Name : null;

                        r.TagNames = r.TagIds
                            .Select(id => _tagMap.TryGetValue(id, out var t)
                                ? t.Name : $"#{id}")
                            .ToList();

                        Results.Add(r);
                    }

                    HasSearched = true;
                    HasResults = Results.Count > 0;
                });

                // Učitaj thumbnailove async
                foreach (var result in Results.ToList())
                    _ = LoadThumbnailAsync(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        // ── Autocomplete ──────────────────────────────────────────────────

        private async Task TriggerAutocompleteAsync(string term)
        {
            // Debounce — čekaj 300ms prije slanja
            _autocompleteCts?.Cancel();
            _autocompleteCts = new CancellationTokenSource();
            var token = _autocompleteCts.Token;

            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                if (term.Length < 2)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Suggestions.Clear();
                        ShowSuggestions = false;
                    });
                    return;
                }

                var suggestions = await _searchService.AutocompleteAsync(term);
                if (token.IsCancellationRequested) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Suggestions.Clear();
                    foreach (var s in suggestions.Take(8))
                        Suggestions.Add(s);
                    ShowSuggestions = Suggestions.Count > 0;
                });
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Autocomplete error: {ex.Message}");
            }
        }

        // ── Thumbnail ─────────────────────────────────────────────────────

        private async Task LoadThumbnailAsync(SearchResult result)
        {
            try
            {
                var bmp = await AuthenticatedImageHelper.LoadImageAsync(
                    result.ThumbnailUrl);
                if (bmp != null)
                    Application.Current.Dispatcher.Invoke(
                        () => result.Thumbnail = bmp);
            }
            catch { }
        }
    }
}