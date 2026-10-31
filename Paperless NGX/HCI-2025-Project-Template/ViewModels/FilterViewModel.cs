using HCI_2025_Project_Template.Helpers;
using HCI_2025_Project_Template.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HCI_2025_Project_Template.ViewModels
{
    /// <summary>
    /// Drži trenutno aktivne filtere i izlaže ih MainViewModelu.
    /// </summary>
    public class FilterViewModel : ObservableObject
    {
        // ── Active filter IDs ─────────────────────────────────────────────
        private int? _selectedTagId;
        private int? _selectedCorrespondentId;
        private int? _selectedDocumentTypeId;
        private bool _hasActiveFilters;

        public int? SelectedTagId
        {
            get => _selectedTagId;
            set { _selectedTagId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTagFilter)); UpdateHasFilters(); }
        }

        public int? SelectedCorrespondentId
        {
            get => _selectedCorrespondentId;
            set { _selectedCorrespondentId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCorrespondentFilter)); UpdateHasFilters(); }
        }

        public int? SelectedDocumentTypeId
        {
            get => _selectedDocumentTypeId;
            set { _selectedDocumentTypeId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDocumentTypeFilter)); UpdateHasFilters(); }
        }

        public bool HasActiveFilters
        {
            get => _hasActiveFilters;
            private set { _hasActiveFilters = value; OnPropertyChanged(); }
        }

        // ── Dropdown source lists ─────────────────────────────────────────
        public ObservableCollection<Tag> Tags { get; } = new();
        public ObservableCollection<Correspondent> Correspondents { get; } = new();
        public ObservableCollection<DocumentType> DocumentTypes { get; } = new();

        // ── Commands ──────────────────────────────────────────────────────
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand ApplyFiltersCommand { get; }

        /// <summary>Brise jedan filter, da korisnik ne mora resetovati sve.</summary>
        public RelayCommand<string> ClearOneCommand { get; }

        public bool HasTagFilter => _selectedTagId.HasValue;
        public bool HasCorrespondentFilter => _selectedCorrespondentId.HasValue;
        public bool HasDocumentTypeFilter => _selectedDocumentTypeId.HasValue;

        // Poziva se kad se filter promijeni — MainViewModel se pretplaćuje
        public System.Action? FiltersChanged { get; set; }

        public FilterViewModel()
        {
            ClearFiltersCommand = new RelayCommand(_ => ClearAll());

            // Filteri se primjenjuju tek na klik, da korisnik moze
            // podesiti vise njih odjednom bez ucitavanja nakon svakog
            ApplyFiltersCommand = new RelayCommand(_ => FiltersChanged?.Invoke());

            ClearOneCommand = new RelayCommand<string>(which =>
            {
                switch (which)
                {
                    case "tag": SelectedTagId = null; break;
                    case "correspondent": SelectedCorrespondentId = null; break;
                    case "type": SelectedDocumentTypeId = null; break;
                }
                FiltersChanged?.Invoke();
            });
        }

        // ── API ───────────────────────────────────────────────────────────

        public void Populate(
            IEnumerable<Tag> tags,
            IEnumerable<Correspondent> correspondents,
            IEnumerable<DocumentType> docTypes)
        {
            Tags.Clear();
            foreach (var t in tags.OrderBy(t => t.Name)) Tags.Add(t);

            Correspondents.Clear();
            foreach (var c in correspondents.OrderBy(c => c.Name)) Correspondents.Add(c);

            DocumentTypes.Clear();
            foreach (var d in docTypes.OrderBy(d => d.Name)) DocumentTypes.Add(d);
        }

        public void ClearAll()
        {
            _selectedTagId = null;
            _selectedCorrespondentId = null;
            _selectedDocumentTypeId = null;
            OnPropertyChanged(nameof(SelectedTagId));
            OnPropertyChanged(nameof(SelectedCorrespondentId));
            OnPropertyChanged(nameof(SelectedDocumentTypeId));
            UpdateHasFilters();
            FiltersChanged?.Invoke();
        }

        /// <summary>
        /// Izgradi query string za API poziv na osnovu aktivnih filtera.
        /// </summary>
        public string BuildQueryString()
        {
            var parts = new List<string>();
            if (SelectedTagId.HasValue)
                parts.Add($"tags__id__in={SelectedTagId.Value}");
            if (SelectedCorrespondentId.HasValue)
                parts.Add($"correspondent__id={SelectedCorrespondentId.Value}");
            if (SelectedDocumentTypeId.HasValue)
                parts.Add($"document_type__id={SelectedDocumentTypeId.Value}");
            return string.Join("&", parts);
        }

        private void UpdateHasFilters()
        {
            HasActiveFilters =
                SelectedTagId.HasValue ||
                SelectedCorrespondentId.HasValue ||
                SelectedDocumentTypeId.HasValue;
        }
    }
}