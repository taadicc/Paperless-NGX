using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HCI_2025_Project_Template.Services
{
    /// <summary>
    /// Licna zbirka sacuvanih dokumenata. Cuva se lokalno, uz korisnicki
    /// profil, i ne dira podatke na serveru - zato ne zahtijeva nikakvu
    /// izmjenu tudje instance Paperless-a niti pravi lazne oznake.
    /// </summary>
    public static class FavoritesService
    {
        private static readonly HashSet<int> _ids = new();

        private static string FilePath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PaperlessNGX");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "favorites.json");
            }
        }

        /// <summary>Javlja se pri svakoj promjeni, da se prikaz osvjezi.</summary>
        public static event Action? Changed;

        public static int Count => _ids.Count;

        public static bool HasAny => _ids.Count > 0;

        public static IReadOnlyCollection<int> Ids => _ids;

        public static bool Contains(int id) => _ids.Contains(id);

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;

                var json = File.ReadAllText(FilePath);
                var ids = JsonSerializer.Deserialize<List<int>>(json);

                _ids.Clear();
                if (ids != null)
                    foreach (var id in ids) _ids.Add(id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[sacuvano] citanje: {ex.Message}");
            }
        }

        /// <summary>Dodaje ili uklanja dokument. Vraca novo stanje.</summary>
        public static bool Toggle(int id)
        {
            bool nowSaved;

            if (_ids.Contains(id))
            {
                _ids.Remove(id);
                nowSaved = false;
            }
            else
            {
                _ids.Add(id);
                nowSaved = true;
            }

            Save();
            Changed?.Invoke();
            return nowSaved;
        }

        public static void Clear()
        {
            _ids.Clear();
            Save();
            Changed?.Invoke();
        }

        /// <summary>Filter za API: id__in=3,17,42</summary>
        public static string BuildFilterQuery() =>
            _ids.Count == 0 ? string.Empty : "id__in=" + string.Join(",", _ids);

        private static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(_ids.ToList()));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[sacuvano] upis: {ex.Message}");
            }
        }
    }
}