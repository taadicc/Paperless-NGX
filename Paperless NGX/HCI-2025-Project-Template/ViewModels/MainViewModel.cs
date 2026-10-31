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
    /// <summary>Jedan broj u traci za paginaciju, ili "..." izmedju skokova.</summary>
    public class PageItem
    {
        public int Number { get; }
        public string Label { get; }
        public bool IsCurrent { get; }
        public bool IsClickable { get; }

        public PageItem(int number, bool isCurrent)
        {
            Number = number;
            Label = number.ToString();
            IsCurrent = isCurrent;
            IsClickable = !isCurrent;
        }

        private PageItem()
        {
            Number = 0;
            Label = "\u2026";
            IsCurrent = false;
            IsClickable = false;
        }

        public static PageItem Gap() => new();
    }

    public class MainViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;

        // Lookup mape — dijele se sa DashboardViewModel-om
        private Dictionary<int, Tag> _tagMap = new();
        private Dictionary<int, DocumentType> _docTypeMap = new();
        private Dictionary<int, Correspondent> _correspondentMap = new();

        // ── State ─────────────────────────────────────────────────────────
        private Statistics _stats = new();
        private DocumentDetail? _selectedDocument;
        private bool _isBusy;
        private bool _isGridView = true;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalDocuments;
        private string _searchQuery = string.Empty;
        private object _currentView = null!;
        private string _activeSection = "dashboard";
        private bool _onlyFavorites;

        private FilterViewModel _filterVm = new();
        private UploadViewModel? _uploadVm;
        private SettingsViewModel? _settingsVm;

        private bool _showAutocompleteSuggestions;
        private System.Threading.CancellationTokenSource? _autocompleteCts;

        // ── Kolekcije ─────────────────────────────────────────────────────
        public ObservableCollection<Document> Documents { get; } = new();

        public FilterViewModel Filter => _filterVm;
        public bool IsDetailView => CurrentView is DocumentDetailViewModel;

        /// <summary>Koji je ekran trenutno aktivan - meni ga istice.</summary>
        public string ActiveSection
        {
            get => _activeSection;
            set
            {
                _activeSection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowSidePanel));
                OnPropertyChanged(nameof(ListTitle));
            }
        }

        /// <summary>
        /// Desni panel ima smisla samo uz listu dokumenata i rezultate pretrage.
        /// Na kontrolnoj tabli i u podesavanjima samo bi oduzimao prostor.
        /// </summary>
        /// <summary>Naslov liste - "Dokumenti" ili "Sačuvano".</summary>
        public string ListTitle => _onlyFavorites
            ? LocalizationService.Text("L_Saved", "Sačuvano")
            : LocalizationService.Text("L_Documents", "Dokumenti");

        public bool ShowSidePanel =>
            !IsDetailView && (_activeSection == "documents" || _activeSection == "search");

        public ObservableCollection<string> AutocompleteSuggestions { get; } = new();

        /// <summary>Brojevi stranica za paginaciju (1 2 3 ... 9).</summary>
        public ObservableCollection<PageItem> PageNumbers { get; } = new();

        /// <summary>Posljednje pretrage, prikazuju se kad je polje prazno.</summary>
        public ObservableCollection<string> RecentSearches { get; } = new();

        private bool _showRecentSearches;
        public bool ShowRecentSearches
        {
            get => _showRecentSearches;
            set { _showRecentSearches = value; OnPropertyChanged(); }
        }

        // ── Properties ────────────────────────────────────────────────────
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDetailView));
                OnPropertyChanged(nameof(ShowSidePanel));
            }
        }

        public bool ShowAutocompleteSuggestions
        {
            get => _showAutocompleteSuggestions;
            set { _showAutocompleteSuggestions = value; OnPropertyChanged(); }
        }

        public bool IsGridView
        {
            get => _isGridView;
            set
            {
                _isGridView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTableView));
            }
        }

        public bool IsTableView => !IsGridView;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RangeLabel));
                RebuildPageNumbers();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); RebuildPageNumbers(); }
        }

        public int TotalDocuments
        {
            get => _totalDocuments;
            set { _totalDocuments = value; OnPropertyChanged(); OnPropertyChanged(nameof(RangeLabel)); }
        }

        /// <summary>Npr. "26-50 / 145" - korisnik uvijek zna gdje se nalazi.</summary>
        public string RangeLabel
        {
            get
            {
                if (TotalDocuments == 0) return string.Empty;
                int from = (CurrentPage - 1) * 25 + 1;
                int to = System.Math.Min(CurrentPage * 25, TotalDocuments);
                return $"{from}\u2013{to} / {TotalDocuments}";
            }
        }

        public Statistics Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        public DocumentDetail? SelectedDocument
        {
            get => _selectedDocument;
            set { _selectedDocument = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                _ = TriggerAutocompleteAsync(value);
            }
        }

        // ── Commands ──────────────────────────────────────────────────────
        public RelayCommand GoToDashboardCommand { get; }
        public RelayCommand GoToDocumentsCommand { get; }
        public RelayCommand ToggleViewCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand ShowDocumentsCommand { get; }
        public RelayCommand SelectDocumentCommand { get; }
        public RelayCommand OpenDetailWindowCommand { get; }
        public RelayCommand DownloadCommand { get; }
        public RelayCommand PreviewCommand { get; }
        public RelayCommand UploadCommand { get; }
        public RelayCommand GoToSearchCommand { get; }
        public RelayCommand GoToTagsCommand { get; }
        public RelayCommand GoToSettingsCommand { get; }
        public RelayCommand GoToUploadCommand { get; }
        public RelayCommand UseAutocompleteCommand { get; }
        public RelayCommand UseRecentSearchCommand { get; }
        public RelayCommand RemoveRecentSearchCommand { get; }
        public RelayCommand ClearSearchHistoryCommand { get; }
        public RelayCommand ShowHistoryCommand { get; }
        public RelayCommand GoToPageCommand { get; }
        public RelayCommand CloseSearchPopupsCommand { get; }
        public RelayCommand GoToFavoritesCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand DownloadByIdCommand { get; }
        public RelayCommand ToggleFavoriteCommand { get; }

        /// <summary>Broj sacuvanih dokumenata, prikazuje se uz stavku menija.</summary>
        public int FavoritesCount => FavoritesService.Count;

        public bool HasFavorites => FavoritesService.HasAny;

        /// <summary>Trazi od prozora da postavi fokus u polje pretrage.</summary>
        public event Action? FocusSearchRequested;

        public void RequestSearchFocus() => FocusSearchRequested?.Invoke();

        // ── Podređeni ViewModeli (lazy) ───────────────────────────────────
        private DashboardViewModel? _dashboardVm;
        private TagsViewModel? _tagsVm;
        private SearchViewModel? _searchVm;

        // ── Constructor ───────────────────────────────────────────────────
        public MainViewModel()
        {
            _documentService = new DocumentService();

            // Navigacija
            GoToDashboardCommand = new RelayCommand(_ => ShowDashboard());
            GoToTagsCommand = new RelayCommand(_ => ShowTags());

            GoToDocumentsCommand = new RelayCommand(async _ =>
            {
                bool cameFromFavorites = _onlyFavorites;

                ShowDocuments();

                // Uvijek ucitaj kad dolazimo iz Sacuvanog, inace bi ostao
                // aktivan filter id__in i vidjeli bismo samo sacuvane
                if (cameFromFavorites || !Documents.Any())
                {
                    CurrentPage = 1;
                    await LoadDocuments();
                }
            });

            GoToSettingsCommand = new RelayCommand(_ => ShowSettings());
            GoToUploadCommand = new RelayCommand(_ => ShowUpload());

            // Ranije je vodio na poruku "dolazi uskoro" — sada vodi na pravi ekran
            UploadCommand = GoToUploadCommand;

            GoToSearchCommand = new RelayCommand(param =>
            {
                var query = param as string ?? string.Empty;
                SearchHistoryService.Add(query);
                RefreshRecentSearches();
                ShowSearch(query);
            });

            // Dokumenti
            ToggleViewCommand = new RelayCommand(_ => IsGridView = !IsGridView);

            UseRecentSearchCommand = new RelayCommand(param =>
            {
                if (param is string q)
                {
                    SearchQuery = q;
                    ShowRecentSearches = false;
                    ShowSearch(q);
                }
            });

            RemoveRecentSearchCommand = new RelayCommand(param =>
            {
                if (param is string q)
                {
                    SearchHistoryService.Remove(q);
                    RefreshRecentSearches(show: true);
                }
            });

            ClearSearchHistoryCommand = new RelayCommand(_ =>
            {
                SearchHistoryService.Clear();
                RefreshRecentSearches();
            });

            // Fokus na prazno polje otvara istoriju
            ShowHistoryCommand = new RelayCommand(_ =>
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                    RefreshRecentSearches(show: true);
            });

            UseAutocompleteCommand = new RelayCommand(param =>
            {
                if (param is string suggestion)
                {
                    SearchQuery = suggestion;
                    ShowAutocompleteSuggestions = false;
                    ShowSearch(suggestion);
                }
            });

            NextPageCommand = new RelayCommand(async _ =>
            {
                if (CurrentPage < TotalPages)
                {
                    CurrentPage++;
                    await LoadDocuments();
                }
            });

            PreviousPageCommand = new RelayCommand(async _ =>
            {
                if (CurrentPage > 1)
                {
                    CurrentPage--;
                    await LoadDocuments();
                }
            });

            SearchCommand = new RelayCommand(async _ =>
            {
                CurrentPage = 1;
                await LoadDocuments();
            });

            // Kompatibilnost sa starim bindingom
            ShowDocumentsCommand = GoToDocumentsCommand;

            SelectDocumentCommand = new RelayCommand(async param =>
            {
                if (param is int id)
                    await LoadSelectedDocumentForPanel(id);
            });

            OpenDetailWindowCommand = new RelayCommand(async param =>
            {
                if (param is int id)
                    await LoadDocumentDetail(id);
            });

            DownloadCommand = new RelayCommand(_ =>
            {
                if (SelectedDocument == null)
                {
                    Notifier.Info("Nijedan dokument nije izabran",
                        "Kliknite na dokument u listi pa pokušajte ponovo.");
                    return;
                }
                OpenUrlInBrowser(_documentService.GetDownloadUrl(SelectedDocument.Id));
            });

            PreviewCommand = new RelayCommand(_ =>
            {
                if (SelectedDocument == null)
                {
                    Notifier.Info("Nijedan dokument nije izabran",
                        "Kliknite na dokument u listi pa pokušajte ponovo.");
                    return;
                }
                OpenUrlInBrowser(_documentService.GetPreviewUrl(SelectedDocument.Id));
            });

            GoToPageCommand = new RelayCommand(async param =>
            {
                if (param is int page && page != CurrentPage && page >= 1 && page <= TotalPages)
                {
                    CurrentPage = page;
                    await LoadDocuments();
                }
            });

            // F5 - osvjezi trenutni prikaz
            RefreshCommand = new RelayCommand(async _ =>
            {
                await LoadDocuments();
                if (_dashboardVm != null) await _dashboardVm.LoadAsync();
                Notifier.Info("Osvježeno", "Lista je ponovo učitana.");
            });

            // Desni klik -> Preuzmi, bez otvaranja dokumenta
            DownloadByIdCommand = new RelayCommand(async param =>
            {
                if (param is not int id) return;

                var doc = Documents.FirstOrDefault(d => d.Id == id);
                var title = doc?.Title ?? $"dokument_{id}";

                var safe = string.Join("_", title.Split(System.IO.Path.GetInvalidFileNameChars()));
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{safe}.pdf",
                    Filter = "PDF dokument (*.pdf)|*.pdf|Svi fajlovi (*.*)|*.*",
                    Title = "Sačuvaj dokument"
                };

                if (dialog.ShowDialog() != true) return;

                if (await _documentService.SaveDocumentAsync(id, dialog.FileName))
                    Notifier.Success("Dokument je sačuvan",
                        System.IO.Path.GetFileName(dialog.FileName));
            });

            GoToFavoritesCommand = new RelayCommand(async _ =>
            {
                _onlyFavorites = true;
                ActiveSection = "favorites";
                CurrentView = this;
                CurrentPage = 1;
                _searchQuery = string.Empty;
                OnPropertyChanged(nameof(SearchQuery));
                _filterVm.ClearAll();
                await LoadDocuments();
            });

            ToggleFavoriteCommand = new RelayCommand(async param =>
            {
                int id = param switch
                {
                    int i => i,
                    Document d => d.Id,
                    _ => 0
                };
                if (id == 0) return;

                bool saved = FavoritesService.Toggle(id);

                // Osvjezi zvjezdicu na svim mjestima gdje se dokument pojavljuje
                foreach (var doc in Documents.Where(x => x.Id == id))
                    doc.IsFavorite = saved;

                if (SelectedDocument?.Id == id)
                    SelectedDocument.IsFavorite = saved;

                OnPropertyChanged(nameof(FavoritesCount));
                OnPropertyChanged(nameof(HasFavorites));

                Notifier.Success(
                    saved ? "Dokument je sačuvan" : "Dokument je uklonjen",
                    saved ? "Naći ćete ga u sekciji Sačuvano."
                          : "Više nije u sekciji Sačuvano.");

                // U sekciji Sacuvano uklonjeni dokument treba odmah da nestane
                if (_onlyFavorites && !saved)
                    await LoadDocuments();
            });

            CloseSearchPopupsCommand = new RelayCommand(_ =>
            {
                ShowRecentSearches = false;
                ShowAutocompleteSuggestions = false;
            });

            // Glasovne komande gadjaju iste radnje kao dugmad i precice
            VoiceCommandService.CommandRecognized += command =>
            {
                switch (command)
                {
                    case "upload":
                        GoToUploadCommand.Execute(null);
                        Notifier.Success("Glasovna komanda", "Otvaram dodavanje dokumenta.");
                        break;

                    case "refresh":
                        RefreshCommand.Execute(null);
                        break;

                    case "search":
                        GoToDocumentsCommand.Execute(null);
                        FocusSearchRequested?.Invoke();
                        Notifier.Success("Glasovna komanda", "Polje za pretragu je spremno.");
                        break;
                }
            };

            FavoritesService.Load();
            SearchHistoryService.Load();
            RefreshRecentSearches();

            _ = InitializeData();
        }

        // ── Navigacija ────────────────────────────────────────────────────

        private void ShowSearch(string initialQuery = "")
        {
            ActiveSection = "search";
            _onlyFavorites = false;
            ShowAutocompleteSuggestions = false;

            if (_searchVm == null)
            {
                _searchVm = new SearchViewModel(
                    _tagMap, _docTypeMap, _correspondentMap, initialQuery);

                _searchVm.OnDocumentSelected = async id =>
                    await LoadSelectedDocumentForPanel(id);

                _searchVm.OnDocumentOpened = async id =>
                    await LoadDocumentDetail(id);
            }
            else if (!string.IsNullOrEmpty(initialQuery))
            {
                _searchVm.UpdateQueryAndSearch(initialQuery);
            }

            CurrentView = _searchVm;
        }

        private void ShowTags()
        {
            ActiveSection = "tags";
            if (_tagsVm == null)
            {
                _tagsVm = new TagsViewModel();

                // Klik na strelicu u kartici oznake vodi na filtriranu listu
                _tagsVm.OnShowDocuments = async tag =>
                {
                    _filterVm.SelectedTagId = tag.Id;
                    ShowDocuments();
                    CurrentPage = 1;
                    await LoadDocuments();
                };
            }

            CurrentView = _tagsVm;
        }

        private void ShowSettings()
        {
            ActiveSection = "settings";
            _settingsVm ??= new SettingsViewModel();
            CurrentView = _settingsVm;
        }

        private void ShowUpload()
        {
            ActiveSection = "upload";
            _uploadVm = new UploadViewModel(
                _documentService, _tagMap, _docTypeMap, _correspondentMap);

            var previousView = CurrentView;
            _uploadVm.GoBackRequested = () => CurrentView = previousView;
            _uploadVm.UploadCompleted = async () =>
            {
                // Bez ovoga bi novi dokument upao u staru pretragu ili filter
                // i korisniku bi izgledalo da je lista ostala prazna
                _searchQuery = string.Empty;
                OnPropertyChanged(nameof(SearchQuery));
                _filterVm.ClearAll();
                CurrentPage = 1;

                await LoadDocuments();
                await LoadStatistics();
            };

            CurrentView = _uploadVm;
        }

        private void ShowDashboard()
        {
            ActiveSection = "dashboard";
            _onlyFavorites = false;
            if (_dashboardVm == null)
            {
                _dashboardVm = new DashboardViewModel(
                    _documentService, _tagMap, _docTypeMap, _correspondentMap);

                _dashboardVm.OnDocumentSelected = async id => await LoadDocumentDetail(id);
                _dashboardVm.OnDocumentOpened = async id => await LoadDocumentDetail(id);
            }

            CurrentView = _dashboardVm;
        }

        private void ShowDocuments()
        {
            ActiveSection = "documents";
            _onlyFavorites = false;
            CurrentView = this;
        }

        // ── Init ──────────────────────────────────────────────────────────

        private async Task InitializeData()
        {
            IsBusy = true;
            try
            {
                await Task.WhenAll(LoadLookups(), LoadStatistics());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _filterVm.Populate(
                        _tagMap.Values,
                        _correspondentMap.Values,
                        _docTypeMap.Values);

                    _filterVm.FiltersChanged = async () =>
                    {
                        CurrentPage = 1;
                        await LoadDocuments();
                    };
                });

                Application.Current.Dispatcher.Invoke(ShowDashboard);
            }
            catch (Exception ex)
            {
                Notifier.Error("pokretanje aplikacije", ex);
            }
            finally { IsBusy = false; }
        }

        // ── Lookups ───────────────────────────────────────────────────────

        private async Task LoadLookups()
        {
            try
            {
                var tagsTask = _documentService.GetTagsAsync();
                var typesTask = _documentService.GetDocumentTypesAsync();
                var corrTask = _documentService.GetCorrespondentsAsync();

                await Task.WhenAll(tagsTask, typesTask, corrTask);

                _tagMap = tagsTask.Result.ToDictionary(t => t.Id);
                _docTypeMap = typesTask.Result.ToDictionary(t => t.Id);
                _correspondentMap = corrTask.Result.ToDictionary(c => c.Id);
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje šifarnika", ex);
            }
        }

        // Autocomplete namjerno NE prijavljuje greske korisniku —
        // prekinuti zahtjevi su ocekivani i zatrpali bi ekran obavjestenjima.
        private async Task TriggerAutocompleteAsync(string term)
        {
            _autocompleteCts?.Cancel();
            _autocompleteCts = new System.Threading.CancellationTokenSource();
            var token = _autocompleteCts.Token;

            try
            {
                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                if (term.Length < 2)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AutocompleteSuggestions.Clear();
                        ShowAutocompleteSuggestions = false;
                        RefreshRecentSearches(show: ShowRecentSearches);
                    });
                    return;
                }

                var searchService = new SearchService();
                var suggestions = await searchService.AutocompleteAsync(term);
                if (token.IsCancellationRequested) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AutocompleteSuggestions.Clear();
                    foreach (var item in suggestions.Take(8))
                        AutocompleteSuggestions.Add(item);
                    ShowAutocompleteSuggestions = AutocompleteSuggestions.Count > 0;
                    ShowRecentSearches = false;
                });
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autocomplete: {ex.Message}");
            }
        }

        /// <summary>
        /// Gradi kratku listu brojeva oko trenutne stranice.
        /// Kod 50 stranica nema smisla ispisati svih 50, pa prikazujemo
        /// prvu, posljednju i nekoliko oko trenutne, sa "..." izmedju.
        /// </summary>
        private void RebuildPageNumbers()
        {
            PageNumbers.Clear();
            if (TotalPages <= 1) return;

            var pages = new System.Collections.Generic.SortedSet<int> { 1, TotalPages };
            for (int p = CurrentPage - 2; p <= CurrentPage + 2; p++)
                if (p >= 1 && p <= TotalPages) pages.Add(p);

            int previous = 0;
            foreach (var p in pages)
            {
                if (previous != 0 && p - previous > 1)
                    PageNumbers.Add(PageItem.Gap());

                PageNumbers.Add(new PageItem(p, p == CurrentPage));
                previous = p;
            }
        }

        /// <summary>
        /// Osvjezava listu nedavnih pretraga. Lista se otvara samo kada
        /// korisnik stane u polje pretrage, ne sama od sebe pri pokretanju.
        /// </summary>
        private void RefreshRecentSearches(bool show = false)
        {
            RecentSearches.Clear();
            foreach (var item in SearchHistoryService.Items)
                RecentSearches.Add(item);

            if (show)
                ShowRecentSearches = RecentSearches.Count > 0
                                     && string.IsNullOrWhiteSpace(SearchQuery);
            else if (RecentSearches.Count == 0)
                ShowRecentSearches = false;
        }

        // ── Dokumenti ─────────────────────────────────────────────────────

        public async Task LoadDocuments()
        {
            IsBusy = true;
            try
            {
                var filterQuery = _filterVm.BuildQueryString();

                if (_onlyFavorites)
                {
                    // Bez ijednog sacuvanog dokumenta nema sta da se trazi
                    if (!FavoritesService.HasAny)
                    {
                        Application.Current.Dispatcher.Invoke(() => Documents.Clear());
                        TotalDocuments = 0;
                        TotalPages = 1;
                        return;
                    }

                    var fav = FavoritesService.BuildFilterQuery();
                    filterQuery = string.IsNullOrEmpty(filterQuery)
                        ? fav : filterQuery + "&" + fav;
                }
                var response = await _documentService.GetDocumentsAsync(
                    CurrentPage, SearchQuery, filterQuery);

                TotalDocuments = response.Count;
                TotalPages = (int)Math.Ceiling(response.Count / 25.0);
                if (TotalPages < 1) TotalPages = 1;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Documents.Clear();
                    foreach (var doc in response.Results)
                    {
                        ResolveNames(doc);
                        doc.IsFavorite = FavoritesService.Contains(doc.Id);
                        Documents.Add(doc);
                    }
                });

                var snapshot = Documents.ToList();
                foreach (var doc in snapshot)
                    _ = LoadThumbnailAsync(doc);
            }
            catch (Exception ex)
            {
                Notifier.Error("prikaz liste dokumenata", ex);
            }
            finally { IsBusy = false; }
        }

        private async Task LoadSelectedDocumentForPanel(int id)
        {
            IsBusy = true;
            try
            {
                var detail = await _documentService.GetDocumentByIdAsync(id);
                if (detail == null) return;

                ResolveNames(detail);
                detail.IsFavorite = FavoritesService.Contains(detail.Id);
                Application.Current.Dispatcher.Invoke(() => SelectedDocument = detail);
                _ = LoadThumbnailAsync(detail);
            }
            catch (Exception ex)
            {
                Notifier.Error("prikaz detalja dokumenta", ex);
            }
            finally { IsBusy = false; }
        }

        private async Task LoadDocumentDetail(int id)
        {
            IsBusy = true;
            try
            {
                var detail = await _documentService.GetDocumentByIdAsync(id);
                if (detail == null) return;

                ResolveNames(detail);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SelectedDocument = detail;

                    var detailVm = new DocumentDetailViewModel(detail);

                    var previousView = CurrentView;
                    detailVm.GoBackRequested = () => CurrentView = previousView;

                    CurrentView = detailVm;
                });
            }
            catch (Exception ex)
            {
                Notifier.Error("otvaranje dokumenta", ex);
            }
            finally { IsBusy = false; }
        }

        // ── Thumbnail ─────────────────────────────────────────────────────
        // Neuspjela slicica nije greska vrijedna prekidanja korisnika.

        private async Task LoadThumbnailAsync(Document doc)
        {
            try
            {
                var bmp = await AuthenticatedImageHelper.LoadImageAsync(doc.ThumbnailUrl);
                if (bmp != null)
                    Application.Current.Dispatcher.Invoke(() => doc.Thumbnail = bmp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Thumbnail #{doc.Id}: {ex.Message}");
            }
        }

        // ── Statistika ────────────────────────────────────────────────────

        public async Task LoadStatistics()
        {
            try
            {
                var s = await _documentService.GetStatisticsAsync();
                if (s != null) Stats = s;
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje statistike", ex);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void ResolveNames(Document doc)
        {
            doc.DocumentTypeName = doc.DocumentTypeId.HasValue
                && _docTypeMap.TryGetValue(doc.DocumentTypeId.Value, out var dt)
                ? dt.Name : "—";

            doc.CorrespondentName = doc.CorrespondentId.HasValue
                && _correspondentMap.TryGetValue(doc.CorrespondentId.Value, out var co)
                ? co.Name : null;

            doc.TagNames = doc.TagIds
                .Select(id => _tagMap.TryGetValue(id, out var t) ? t.Name : $"#{id}")
                .ToList();
        }

        private void OpenUrlInBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                Notifier.Error("otvaranje dokumenta u pregledaču", ex);
            }
        }
    }
}