using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

namespace HCI_2025_Project_Template.Services
{
    /// <summary>
    /// Mijenja jezik korisnickog sucelja u toku rada, bez ponovnog pokretanja.
    /// Rjecnici sa natpisima se ucitavaju kao MergedDictionaries, pa svi
    /// natpisi vezani preko DynamicResource odmah dobiju novu vrijednost.
    /// </summary>
    public static class LocalizationService
    {
        private const string RegKey = @"SOFTWARE\PaperlessNGX";

        public const string Serbian = "sr";
        public const string English = "en";

        public static string CurrentLanguage { get; private set; } = Serbian;

        /// <summary>Javlja se kad se jezik promijeni — da se osvjeze natpisi iz koda.</summary>
        public static event Action<string>? LanguageChanged;

        /// <summary>Kratka oznaka za prikaz u zaglavlju: SR ili EN.</summary>
        public static string ShortLabel =>
            CurrentLanguage == English ? "EN" : "SR";

        /// <summary>Ucitava sacuvani jezik pri pokretanju aplikacije.</summary>
        public static void LoadSaved()
        {
            var code = Serbian;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey);
                code = (string?)key?.GetValue("Language", Serbian) ?? Serbian;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[jezik] citanje: {ex.Message}");
            }

            Apply(code, save: false);
        }

        /// <summary>Prebacuje na drugi jezik — za dugme u zaglavlju.</summary>
        public static void Toggle()
            => Apply(CurrentLanguage == Serbian ? English : Serbian);

        public static void Apply(string languageCode, bool save = true)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                languageCode = Serbian;

            var app = Application.Current;
            if (app == null) return;

            try
            {
                var dict = new ResourceDictionary
                {
                    Source = new Uri(
                        $"pack://application:,,,/Resources/Strings.{languageCode}.xaml",
                        UriKind.Absolute)
                };

                var merged = app.Resources.MergedDictionaries;

                // Ukloni prethodni jezicki rjecnik - prepoznajemo ga po markeru
                var previous = merged.FirstOrDefault(d => d.Contains("L_LanguageMarker"));
                if (previous != null)
                    merged.Remove(previous);

                merged.Add(dict);
                CurrentLanguage = languageCode;

                if (save) SaveToRegistry(languageCode);

                LanguageChanged?.Invoke(languageCode);
            }
            catch (Exception ex)
            {
                // Ako trazeni jezik ne postoji, ostajemo na trenutnom
                System.Diagnostics.Debug.WriteLine($"[jezik] {languageCode}: {ex.Message}");
                Notifier.Warning("Jezik nije promijenjen",
                    "Odabrani jezik trenutno nije dostupan.");
            }
        }

        private static void SaveToRegistry(string code)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegKey);
                key?.SetValue("Language", code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[jezik] upis: {ex.Message}");
            }
        }

        /// <summary>Dohvata natpis iz koda, npr. za poruke u Notifier-u.</summary>
        public static string Text(string key, string fallback = "")
        {
            try
            {
                return Application.Current?.TryFindResource(key) as string ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}