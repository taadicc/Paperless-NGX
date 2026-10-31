using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HCI_2025_Project_Template.Services
{
    /// <summary>
    /// Pamti posljednje pretrage korisnika i cuva ih izmedju pokretanja.
    /// Web verzija Paperless-a ovo nema: svaki upit se mora iznova ukucati.
    /// </summary>
    public static class SearchHistoryService
    {
        private const string RegKey = @"SOFTWARE\PaperlessNGX";
        private const string ValueName = "SearchHistory";
        private const char Separator = '\u001F';   // razdvajac koji se ne pojavljuje u tekstu
        private const int MaxItems = 8;

        private static readonly List<string> _items = new();

        public static event Action? Changed;

        public static IReadOnlyList<string> Items => _items;

        public static bool HasItems => _items.Count > 0;

        public static void Load()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey);
                var raw = (string?)key?.GetValue(ValueName, string.Empty) ?? string.Empty;

                _items.Clear();
                foreach (var item in raw.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
                    _items.Add(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[istorija] citanje: {ex.Message}");
            }
        }

        /// <summary>Dodaje upit na vrh liste. Ponovljeni upit se pomjera gore, ne duplira.</summary>
        public static void Add(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            query = query.Trim();
            if (query.Length < 2) return;

            _items.RemoveAll(x => string.Equals(x, query, StringComparison.OrdinalIgnoreCase));
            _items.Insert(0, query);

            while (_items.Count > MaxItems)
                _items.RemoveAt(_items.Count - 1);

            Save();
            Changed?.Invoke();
        }

        public static void Remove(string query)
        {
            if (_items.RemoveAll(x => x == query) > 0)
            {
                Save();
                Changed?.Invoke();
            }
        }

        public static void Clear()
        {
            _items.Clear();
            Save();
            Changed?.Invoke();
        }

        private static void Save()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegKey);
                key?.SetValue(ValueName, string.Join(Separator, _items));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[istorija] upis: {ex.Message}");
            }
        }
    }
}