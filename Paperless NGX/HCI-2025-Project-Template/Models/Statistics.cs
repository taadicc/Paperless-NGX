using System.Text.Json.Serialization;

namespace HCI_2025_Project_Template.Models
{
    public class Statistics
    {
        // Paperless NGX API vraæa ova polja u /api/statistics/
        [JsonPropertyName("documents_total")]
        public int TotalDocuments { get; set; }

        [JsonPropertyName("documents_inbox")]
        public int InboxCount { get; set; }

        [JsonPropertyName("inbox_tag")]
        public int? InboxTag { get; set; }

        [JsonPropertyName("inbox_tags")]
        public int[]? InboxTags { get; set; }

        [JsonPropertyName("document_file_type_counts")]
        public object? DocumentFileTypeCounts { get; set; }

        [JsonPropertyName("character_count")]
        public long CharacterCount { get; set; }

        // tags_total ne postoji direktno u API-u — koristimo poseban poziv
        // Prikazujemo 0 ako nije dostupno
        public int TagsCount { get; set; }
    }
}