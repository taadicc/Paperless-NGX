using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HCI_2025_Project_Template.Services
{
    public enum NotificationKind { Info, Success, Warning, Error }

    public class Notification
    {
        public NotificationKind Kind { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>
    /// Centralno mjesto za prijavu grešaka i poruka korisniku.
    /// Servisi i ViewModeli ovdje prijavljuju problem, a View ga prikazuje.
    /// </summary>
    public static class Notifier
    {
        public static event Action<Notification>? Raised;

        private static void Raise(NotificationKind kind, string title, string message)
        {
            var n = new Notification { Kind = kind, Title = title, Message = message };
            var app = Application.Current;
            if (app == null) return;

            if (app.Dispatcher.CheckAccess())
                Raised?.Invoke(n);
            else
                app.Dispatcher.Invoke(() => Raised?.Invoke(n));
        }

        public static void Info(string title, string message = "")
            => Raise(NotificationKind.Info, title, message);

        public static void Success(string title, string message = "")
            => Raise(NotificationKind.Success, title, message);

        public static void Warning(string title, string message = "")
            => Raise(NotificationKind.Warning, title, message);

        public static void Error(string title, string message = "")
            => Raise(NotificationKind.Error, title, message);

        /// <summary>Prevodi izuzetak u poruku razumljivu korisniku.</summary>
        public static void Error(string context, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{context}] {ex}");

            var (title, message) = ex switch
            {
                HttpRequestException => ("Nema veze sa serverom",
                    "Provjerite internet konekciju i pokušajte ponovo."),
                TaskCanceledException => ("Server ne odgovara",
                    "Zahtjev je predugo trajao."),
                JsonException => ("Neočekivan odgovor servera",
                    "Podaci nisu stigli u očekivanom formatu."),
                _ => ("Došlo je do greške", ex.Message)
            };

            Raise(NotificationKind.Error, title, $"{message} ({context})");
        }

        /// <summary>Prevodi neuspješan HTTP status u poruku razumljivu korisniku.</summary>
        public static void HttpError(string context, HttpStatusCode code)
        {
            System.Diagnostics.Debug.WriteLine($"[{context}] HTTP {(int)code}");

            string message = code switch
            {
                HttpStatusCode.Unauthorized => "Sesija je istekla. Prijavite se ponovo.",
                HttpStatusCode.Forbidden => "Nemate dozvolu za ovu radnju.",
                HttpStatusCode.NotFound => "Traženi podatak ne postoji na serveru.",
                _ when (int)code >= 500 => "Greška na serveru. Pokušajte kasnije.",
                _ => $"Server je odbio zahtjev (HTTP {(int)code})."
            };

            Raise(NotificationKind.Error, "Zahtjev nije uspio", $"{message} ({context})");
        }
    }
}