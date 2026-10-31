using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace HCI_2025_Project_Template.Models
{
    public class Document : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("document_type")]
        public int? DocumentTypeId { get; set; }

        [JsonPropertyName("correspondent")]
        public int? CorrespondentId { get; set; }

        [JsonPropertyName("tags")]
        public List<int> TagIds { get; set; } = new();

        // Computed
        public string CreatedFormatted => Created.ToString("dd.MM.yyyy");
        public string ThumbnailUrl =>
            $"http://paperless-etf.duckdns.org:8000/api/documents/{Id}/thumb/";

        // Thumbnail koji se učitava async sa autentifikacijom
        private BitmapImage? _thumbnail;
        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        // Resolved names (popunjavamo ih u ViewModelu)
        private string? _documentTypeName;
        public string? DocumentTypeName
        {
            get => _documentTypeName;
            set { _documentTypeName = value; OnPropertyChanged(); }
        }

        private string? _correspondentName;
        public string? CorrespondentName
        {
            get => _correspondentName;
            set { _correspondentName = value; OnPropertyChanged(); }
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(); }
        }

        // Tag names lista (popunjavamo u ViewModelu)
        private List<string> _tagNames = new();
        public List<string> TagNames
        {
            get => _tagNames;
            set { _tagNames = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class DocumentType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class Tag
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("colour")]
        public int Colour { get; set; }

        [JsonPropertyName("matching_algorithm")]
        public int MatchingAlgorithm { get; set; }

        [JsonPropertyName("match")]
        public string Match { get; set; } = string.Empty;

        [JsonPropertyName("is_insensitive")]
        public bool IsInsensitive { get; set; }

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        // Hex boja za UI — Paperless šalje integer indeks
        // ali novije verzije šalju hex direktno kao string u "color" fieldu
        [JsonPropertyName("color")]
        public string? ColorHex { get; set; }

        // Computed property za prikaz
        public string DisplayColor => !string.IsNullOrEmpty(ColorHex)
            ? ColorHex
            : Colour switch
            {
                1 => "#a6cee3",
                2 => "#1f78b4",
                3 => "#b2df8a",
                4 => "#33a02c",
                5 => "#fb9a99",
                6 => "#e31a1c",
                7 => "#fdbf6f",
                8 => "#ff7f00",
                9 => "#cab2d6",
                10 => "#6a3d9a",
                11 => "#ffff99",
                12 => "#b15928",
                _ => "#0D9488"
            };

        public string MatchingAlgorithmName => MatchingAlgorithm switch
        {
            0 => "Nijedan",
            1 => "Bilo koji",
            2 => "Sve",
            3 => "Regularni izraz",
            4 => "Fuzzy",
            6 => "Automatski",
            _ => "—"
        };
    }

    public class Correspondent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class DocumentResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("results")]
        public List<Document> Results { get; set; } = new();
    }

    public class TagResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("results")]
        public List<Tag> Results { get; set; } = new();
    }

    public class DocumentTypeResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("results")]
        public List<DocumentType> Results { get; set; } = new();
    }

    public class CorrespondentResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("results")]
        public List<Correspondent> Results { get; set; } = new();
    }
}