using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HCI_2025_Project_Template.Models
{
    public class SearchResponse
    {
        [JsonPropertyName("total")]
        public int Count { get; set; }  // API vraća "total" ne "count"

        [JsonPropertyName("documents")]
        public List<SearchResult> Results { get; set; } = new();  // API vraća "documents" ne "results"
    }

    public class SearchResult
    {
        // Search API vraća iste fielede kao Documents API
        // plus "__search_hit__" metadata
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public System.DateTime Created { get; set; }

        [JsonPropertyName("document_type")]
        public int? DocumentTypeId { get; set; }

        [JsonPropertyName("correspondent")]
        public int? CorrespondentId { get; set; }

        [JsonPropertyName("tags")]
        public List<int> TagIds { get; set; } = new();

        [JsonPropertyName("__search_hit__")]
        public SearchHit? SearchHit { get; set; }

        // Runtime properties
        public string? DocumentTypeName { get; set; }
        public string? CorrespondentName { get; set; }
        public List<string> TagNames { get; set; } = new();
        public string CreatedFormatted =>
            Created.ToString("dd.MM.yyyy");
        public string ThumbnailUrl =>
            $"http://paperless-etf.duckdns.org:8000/api/documents/{Id}/thumb/";

        private System.Windows.Media.Imaging.BitmapImage? _thumbnail;
        public System.Windows.Media.Imaging.BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set => _thumbnail = value;
        }

        // Score 0-1 koliko dobro odgovara pretrazi
        public double Score => SearchHit?.Score ?? 0;
        public string ScorePercent =>
            $"{(int)(Score * 100)}%";
    }

    public class SearchHit
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("highlights")]
        public Dictionary<string, List<string>>? Highlights { get; set; }

        // Koji su dijelovi teksta pronađeni
        public string HighlightText
        {
            get
            {
                if (Highlights == null) return string.Empty;
                var parts = new List<string>();
                foreach (var kv in Highlights)
                    foreach (var h in kv.Value)
                        parts.Add(h);
                return string.Join(" ... ", parts);
            }
        }
    }
}