using System;
using System.Text.Json.Serialization;

namespace HCI_2025_Project_Template.Models
{
    public class DocumentDetail : Document
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }

        [JsonPropertyName("modified")]
        public DateTime Modified { get; set; }

        [JsonPropertyName("page_count")]
        public int? PageCount { get; set; }

        // Formatted helpers
        public string AddedFormatted =>
            Added == default ? "—" : Added.ToString("dd.MM.yyyy HH:mm");

        public string ModifiedFormatted =>
            Modified == default ? "—" : Modified.ToString("dd.MM.yyyy HH:mm");
    }
}