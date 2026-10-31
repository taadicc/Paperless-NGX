using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using HCI_2025_Project_Template.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HCI_2025_Project_Template.ViewModels
{
    /// <summary>Grupa boja za filtriranje oznaka (npr. "Plava", "Zelena").</summary>
    public class ColourGroup
    {
        public string Key { get; }
        public string Label { get; }
        public Brush Swatch { get; }

        public ColourGroup(string key, string label, string hex)
        {
            Key = key;
            Label = label;
            Swatch = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
    }

    public class TagsViewModel : ObservableObject
    {
        private readonly TagService _tagService;

        private const int PageSize = 12;

        // ── State ─────────────────────────────────────────────────────────
        private bool _isBusy;
        private string _searchText = string.Empty;
        private string _selectedSort = string.Empty;
        private string _selectedColourKey = "all";
        private int _currentPage = 1;

        private Tag? _selectedTag;
        private bool _isFormOpen;
        private bool _isEditMode;
        private string _formName = string.Empty;
        private string _formColor = "#0D9488";
        private string _formError = string.Empty;

        // ── Kolekcije ─────────────────────────────────────────────────────
        public ObservableCollection<Tag> Tags { get; } = new();
        public ObservableCollection<Tag> FilteredTags { get; } = new();
        public ObservableCollection<Tag> PagedTags { get; } = new();
        public ObservableCollection<ColourGroup> ColourGroups { get; } = new();

        /// <summary>Otvara listu dokumenata filtriranu po izabranoj oznaci.</summary>
        public Action<Tag>? OnShowDocuments { get; set; }

        // ── Sortiranje ────────────────────────────────────────────────────

        public List<string> SortOptions { get; } = new()
        {
            "Naziv A-Ž",
            "Naziv Ž-A",
            "Najviše dokumenata",
            "Najmanje dokumenata",
            "Po boji"
        };

        public string SelectedSort
        {
            get => _selectedSort;
            set { _selectedSort = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // ── Filtriranje ───────────────────────────────────────────────────

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSearch));
                _currentPage = 1;
                ApplyFilter();
            }
        }

        public bool HasSearch => !string.IsNullOrWhiteSpace(_searchText);

        public string SelectedColourKey
        {
            get => _selectedColourKey;
            set
            {
                _selectedColourKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsColourFiltered));
                _currentPage = 1;
                ApplyFilter();
            }
        }

        public bool IsColourFiltered => _selectedColourKey != "all";

        // ── Paginacija ────────────────────────────────────────────────────

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int TotalPages =>
            Math.Max(1, (int)Math.Ceiling(FilteredTags.Count / (double)PageSize));

        public bool HasPages => FilteredTags.Count > PageSize;

        public bool IsEmpty => !IsBusy && FilteredTags.Count == 0;

        public string RangeLabel
        {
            get
            {
                if (FilteredTags.Count == 0) return string.Empty;
                int from = (_currentPage - 1) * PageSize + 1;
                int to = Math.Min(_currentPage * PageSize, FilteredTags.Count);
                return $"{from}\u2013{to} / {FilteredTags.Count}";
            }
        }

        // ── Forma ─────────────────────────────────────────────────────────

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); }
        }

        public Tag? SelectedTag
        {
            get => _selectedTag;
            set { _selectedTag = value; OnPropertyChanged(); }
        }

        public bool IsFormOpen
        {
            get => _isFormOpen;
            set { _isFormOpen = value; OnPropertyChanged(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(); }
        }

        public string FormName
        {
            get => _formName;
            set { _formName = value; OnPropertyChanged(); }
        }

        public string FormColor
        {
            get => _formColor;
            set { _formColor = value; OnPropertyChanged(); }
        }

        public string FormError
        {
            get => _formError;
            set { _formError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFormError)); }
        }

        public bool HasFormError => !string.IsNullOrWhiteSpace(_formError);

        public string FormTitle => IsEditMode ? "Uredi oznaku" : "Nova oznaka";

        public string[] PresetColors { get; } =
        {
            "#EF4444", "#F97316", "#F59E0B", "#EAB308", "#84CC16",
            "#22C55E", "#10B981", "#0D9488", "#06B6D4", "#0EA5E9",
            "#3B82F6", "#6366F1", "#8B5CF6", "#A855F7", "#D946EF",
            "#EC4899", "#F43F5E", "#78716C", "#64748B", "#0F172A"
        };

        // ── Commands ──────────────────────────────────────────────────────
        public RelayCommand RefreshCommand { get; }
        public RelayCommand OpenCreateFormCommand { get; }
        public RelayCommand OpenEditFormCommand { get; }
        public RelayCommand CloseFormCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand<string> SetColorCommand { get; }
        public RelayCommand ClearSearchCommand { get; }
        public RelayCommand ShowDocumentsCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }
        public RelayCommand<string> FilterByColourCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────

        public TagsViewModel()
        {
            _tagService = new TagService();
            _selectedSort = SortOptions[0];

            BuildColourGroups();

            RefreshCommand = new RelayCommand(async _ => await LoadTagsAsync());

            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);

            FilterByColourCommand = new RelayCommand<string>(key =>
            {
                if (!string.IsNullOrEmpty(key)) SelectedColourKey = key;
            });

            ShowDocumentsCommand = new RelayCommand(param =>
            {
                if (param is Tag tag) OnShowDocuments?.Invoke(tag);
            });

            NextPageCommand = new RelayCommand(_ =>
            {
                if (CurrentPage < TotalPages) { CurrentPage++; ApplyPaging(); }
            });

            PreviousPageCommand = new RelayCommand(_ =>
            {
                if (CurrentPage > 1) { CurrentPage--; ApplyPaging(); }
            });

            OpenCreateFormCommand = new RelayCommand(_ =>
            {
                IsEditMode = false;
                FormName = string.Empty;
                FormColor = "#0D9488";
                FormError = string.Empty;
                IsFormOpen = true;
                OnPropertyChanged(nameof(FormTitle));
            });

            OpenEditFormCommand = new RelayCommand(param =>
            {
                var tag = param as Tag ?? SelectedTag;
                if (tag == null) return;

                SelectedTag = tag;
                IsEditMode = true;
                FormName = tag.Name;
                FormColor = tag.DisplayColor;
                FormError = string.Empty;
                IsFormOpen = true;
                OnPropertyChanged(nameof(FormTitle));
            });

            CloseFormCommand = new RelayCommand(_ =>
            {
                IsFormOpen = false;
                FormError = string.Empty;
            });

            SaveCommand = new RelayCommand(async _ => await SaveTagAsync());

            DeleteCommand = new RelayCommand(async param =>
            {
                var tag = param as Tag ?? SelectedTag;
                if (tag == null) return;

                var result = MessageBox.Show(
                    $"Jeste li sigurni da želite obrisati oznaku \"{tag.Name}\"?",
                    "Potvrda brisanja",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                    await DeleteTagAsync(tag);
            });

            SetColorCommand = new RelayCommand<string>(color =>
            {
                if (!string.IsNullOrEmpty(color)) FormColor = color;
            });

            _ = LoadTagsAsync();
        }

        // ── Grupe boja ────────────────────────────────────────────────────

        private void BuildColourGroups()
        {
            ColourGroups.Clear();
            ColourGroups.Add(new ColourGroup("all", "Sve", "#94A3B8"));
            ColourGroups.Add(new ColourGroup("red", "Crvena", "#EF4444"));
            ColourGroups.Add(new ColourGroup("orange", "Narandžasta", "#F97316"));
            ColourGroups.Add(new ColourGroup("yellow", "Žuta", "#EAB308"));
            ColourGroups.Add(new ColourGroup("green", "Zelena", "#22C55E"));
            ColourGroups.Add(new ColourGroup("teal", "Tirkizna", "#0D9488"));
            ColourGroups.Add(new ColourGroup("blue", "Plava", "#3B82F6"));
            ColourGroups.Add(new ColourGroup("purple", "Ljubičasta", "#8B5CF6"));
            ColourGroups.Add(new ColourGroup("pink", "Roza", "#EC4899"));
            ColourGroups.Add(new ColourGroup("grey", "Siva", "#64748B"));
        }

        /// <summary>
        /// Svrstava boju oznake u grupu na osnovu nijanse (hue) i zasicenosti.
        /// Tako korisnik moze filtrirati "sve plave oznake" bez obzira na
        /// tacnu hex vrijednost.
        /// </summary>
        private static string ColourKeyOf(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) return "grey";
                if (!hex.StartsWith("#")) hex = "#" + hex;
                var c = (Color)ColorConverter.ConvertFromString(hex);

                double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
                double max = Math.Max(r, Math.Max(g, b));
                double min = Math.Min(r, Math.Min(g, b));
                double delta = max - min;

                // Slabo zasicene boje idu u sivu bez obzira na nijansu
                if (delta < 0.12 || max < 0.15) return "grey";

                double hue;
                if (max == r) hue = 60 * (((g - b) / delta) % 6);
                else if (max == g) hue = 60 * (((b - r) / delta) + 2);
                else hue = 60 * (((r - g) / delta) + 4);
                if (hue < 0) hue += 360;

                return hue switch
                {
                    < 15 => "red",
                    < 40 => "orange",
                    < 70 => "yellow",
                    < 150 => "green",
                    < 190 => "teal",
                    < 250 => "blue",
                    < 290 => "purple",
                    < 335 => "pink",
                    _ => "red"
                };
            }
            catch { return "grey"; }
        }

        // ── Data ──────────────────────────────────────────────────────────

        private async Task LoadTagsAsync()
        {
            IsBusy = true;
            try
            {
                var tags = await _tagService.GetAllTagsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Tags.Clear();
                    foreach (var t in tags.OrderBy(t => t.Name))
                        Tags.Add(t);

                    ApplyFilter();
                });
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje oznaka", ex);
            }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var q = (_searchText ?? string.Empty).Trim().ToLowerInvariant();

                IEnumerable<Tag> result = Tags;

                if (!string.IsNullOrEmpty(q))
                    result = result.Where(t => t.Name.ToLowerInvariant().Contains(q));

                if (_selectedColourKey != "all")
                    result = result.Where(t => ColourKeyOf(t.DisplayColor) == _selectedColourKey);

                result = _selectedSort switch
                {
                    "Naziv Ž-A" => result.OrderByDescending(t => t.Name),
                    "Najviše dokumenata" => result.OrderByDescending(t => t.DocumentCount)
                                                  .ThenBy(t => t.Name),
                    "Najmanje dokumenata" => result.OrderBy(t => t.DocumentCount)
                                                   .ThenBy(t => t.Name),
                    "Po boji" => result.OrderBy(t => ColourKeyOf(t.DisplayColor))
                                       .ThenBy(t => t.Name),
                    _ => result.OrderBy(t => t.Name)
                };

                FilteredTags.Clear();
                foreach (var t in result) FilteredTags.Add(t);

                if (_currentPage > TotalPages) _currentPage = TotalPages;
                if (_currentPage < 1) _currentPage = 1;

                ApplyPaging();
            });
        }

        private void ApplyPaging()
        {
            PagedTags.Clear();
            foreach (var t in FilteredTags.Skip((_currentPage - 1) * PageSize).Take(PageSize))
                PagedTags.Add(t);

            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(HasPages));
            OnPropertyChanged(nameof(RangeLabel));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(FilteredTags));
        }

        // ── CRUD ──────────────────────────────────────────────────────────

        private async Task SaveTagAsync()
        {
            if (string.IsNullOrWhiteSpace(FormName))
            {
                FormError = "Naziv oznake je obavezan.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode && SelectedTag != null)
                {
                    var updated = await _tagService.UpdateTagAsync(
                        SelectedTag.Id, FormName.Trim(), FormColor);

                    if (updated != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var existing = Tags.FirstOrDefault(t => t.Id == updated.Id);
                            if (existing != null) Tags[Tags.IndexOf(existing)] = updated;
                            ApplyFilter();
                            IsFormOpen = false;
                        });
                        Notifier.Success("Oznaka je sačuvana", updated.Name);
                    }
                    else FormError = "Greška pri ažuriranju. Pokušajte ponovo.";
                }
                else
                {
                    var created = await _tagService.CreateTagAsync(FormName.Trim(), FormColor);

                    if (created != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Tags.Add(created);
                            ApplyFilter();
                            IsFormOpen = false;
                        });
                        Notifier.Success("Oznaka je kreirana", created.Name);
                    }
                    else FormError = "Greška pri kreiranju. Pokušajte ponovo.";
                }
            }
            catch (Exception ex)
            {
                FormError = $"Greška: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private async Task DeleteTagAsync(Tag tag)
        {
            IsBusy = true;
            try
            {
                var ok = await _tagService.DeleteTagAsync(tag.Id);
                if (ok)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Tags.Remove(tag);
                        ApplyFilter();
                        if (SelectedTag?.Id == tag.Id) SelectedTag = null;
                    });
                    Notifier.Success("Oznaka je obrisana", tag.Name);
                }
                else
                {
                    Notifier.Error("Brisanje nije uspjelo",
                        "Server je odbio zahtjev za brisanje oznake.");
                }
            }
            catch (Exception ex)
            {
                Notifier.Error("brisanje oznake", ex);
            }
            finally { IsBusy = false; }
        }
    }
}
