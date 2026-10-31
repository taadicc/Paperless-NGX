using System;
using System.Text.Json.Serialization;

namespace HCI_2025_Project_Template.Models
{
    public class AuditLogEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("actor")]
        public string? Actor { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("changes")]
        public System.Text.Json.JsonElement? Changes { get; set; }

        public string TimestampFormatted =>
            Timestamp == default ? "—"
            : Timestamp.ToString("dd.MM.yyyy HH:mm");

        public string ActionLabel => Action switch
        {
            "create" => "Kreiran",
            "update" => "Izmijenjen",
            "delete" => "Obrisan",
            _ => Action ?? "—"
        };

        public string ChangesText
        {
            get
            {
                if (Changes == null) return "";
                try { return Changes.Value.ToString(); }
                catch { return ""; }
            }
        }
    }
}