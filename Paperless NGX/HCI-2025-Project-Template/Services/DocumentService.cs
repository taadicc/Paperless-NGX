using HCI_2025_Project_Template.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace HCI_2025_Project_Template.Services
{
    public class DocumentService
    {
        // ── Dokumenti ─────────────────────────────────────────────────────

        public async Task<DocumentResponse> GetDocumentsAsync(
            int page = 1,
            string search = "",
            string filterQuery = "")
        {
            var client = ApiService.GetClient();
            try
            {
                var url = $"documents/?page={page}&page_size=25";

                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={Uri.EscapeDataString(search)}";

                if (!string.IsNullOrWhiteSpace(filterQuery))
                    url += $"&{filterQuery}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Notifier.HttpError("učitavanje dokumenata", response.StatusCode);
                    return new DocumentResponse();
                }

                return await response.Content.ReadFromJsonAsync<DocumentResponse>()
                       ?? new DocumentResponse();
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje dokumenata", ex);
                return new DocumentResponse();
            }
        }

        public async Task<DocumentDetail?> GetDocumentByIdAsync(int id)
        {
            var client = ApiService.GetClient();
            try
            {
                var r = await client.GetAsync($"documents/{id}/");

                if (!r.IsSuccessStatusCode)
                {
                    Notifier.HttpError($"otvaranje dokumenta #{id}", r.StatusCode);
                    return null;
                }

                return await r.Content.ReadFromJsonAsync<DocumentDetail>();
            }
            catch (Exception ex)
            {
                Notifier.Error($"otvaranje dokumenta #{id}", ex);
                return null;
            }
        }

        // ── Statistika ────────────────────────────────────────────────────

        public async Task<Statistics> GetStatisticsAsync()
        {
            var client = ApiService.GetClient();
            var stats = new Statistics();

            try
            {
                var r = await client.GetAsync("statistics/");

                if (r.IsSuccessStatusCode)
                {
                    var raw = await r.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[statistics] {raw}");

                    try
                    {
                        using var doc = JsonDocument.Parse(raw);
                        var root = doc.RootElement;

                        stats.TotalDocuments = ReadInt(root, "documents_total");
                        stats.InboxCount = ReadInt(root, "documents_inbox");
                        stats.CharacterCount = ReadInt(root, "character_count");

                        // Neke verzije Paperless-a vec vracaju broj oznaka
                        var tagCount = ReadInt(root, "tag_count");
                        if (tagCount > 0) stats.TagsCount = tagCount;
                    }
                    catch (JsonException)
                    {
                        // Format nije prepoznat — necemo dizati paniku, imamo rezervni plan
                        System.Diagnostics.Debug.WriteLine("[statistics] nepoznat format, koristim fallback");
                    }
                }
                else
                {
                    Notifier.HttpError("učitavanje statistike", r.StatusCode);
                }

                // Rezervni plan: ako statistics/ nije dao broj dokumenata,
                // procitaj ga iz same liste dokumenata (count uvijek postoji).
                if (stats.TotalDocuments == 0)
                {
                    var docsR = await client.GetAsync("documents/?page_size=1");
                    if (docsR.IsSuccessStatusCode)
                    {
                        var docsResp = await docsR.Content.ReadFromJsonAsync<DocumentResponse>();
                        if (docsResp != null) stats.TotalDocuments = docsResp.Count;
                    }
                }

                // Broj oznaka
                if (stats.TagsCount == 0)
                {
                    var tagsR = await client.GetAsync("tags/?page_size=1");
                    if (tagsR.IsSuccessStatusCode)
                    {
                        var tagsResp = await tagsR.Content.ReadFromJsonAsync<TagResponse>();
                        if (tagsResp != null) stats.TagsCount = tagsResp.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje statistike", ex);
            }

            return stats;
        }

        // ── Pomocne metode za odbrambeno citanje JSON-a ───────────────────────

        private static int ReadInt(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var el)
                && el.ValueKind == JsonValueKind.Number
                && el.TryGetInt32(out var value))
                return value;
            return 0;
        }

        private static long ReadLong(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var el)
                && el.ValueKind == JsonValueKind.Number
                && el.TryGetInt64(out var value))
                return value;
            return 0;
        }

        // ── Lookup podaci ─────────────────────────────────────────────────

        public async Task<List<Tag>> GetTagsAsync()
        {
            var client = ApiService.GetClient();
            var allTags = new List<Tag>();

            try
            {
                // Tagova moze biti vise od jedne stranice — citamo dok ima "next"
                string? url = "tags/?page_size=100";

                while (url != null)
                {
                    var r = await client.GetAsync(url);

                    if (!r.IsSuccessStatusCode)
                    {
                        Notifier.HttpError("učitavanje oznaka", r.StatusCode);
                        break;
                    }

                    var resp = await r.Content.ReadFromJsonAsync<TagResponse>();
                    if (resp == null) break;

                    allTags.AddRange(resp.Results);

                    // Server vraca apsolutni URL — pretvaramo ga u relativni
                    url = resp.Next?.Replace(ApiService.BaseUrl, "");
                }
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje oznaka", ex);
            }

            return allTags;
        }

        public async Task<List<DocumentType>> GetDocumentTypesAsync()
        {
            var client = ApiService.GetClient();
            try
            {
                var r = await client.GetAsync("document_types/?page_size=100");

                if (!r.IsSuccessStatusCode)
                {
                    Notifier.HttpError("učitavanje tipova dokumenata", r.StatusCode);
                    return new List<DocumentType>();
                }

                var resp = await r.Content.ReadFromJsonAsync<DocumentTypeResponse>();
                return resp?.Results ?? new List<DocumentType>();
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje tipova dokumenata", ex);
                return new List<DocumentType>();
            }
        }

        public async Task<List<Correspondent>> GetCorrespondentsAsync()
        {
            var client = ApiService.GetClient();
            try
            {
                var r = await client.GetAsync("correspondents/?page_size=100");

                if (!r.IsSuccessStatusCode)
                {
                    Notifier.HttpError("učitavanje korespondenata", r.StatusCode);
                    return new List<Correspondent>();
                }

                var resp = await r.Content.ReadFromJsonAsync<CorrespondentResponse>();
                return resp?.Results ?? new List<Correspondent>();
            }
            catch (Exception ex)
            {
                Notifier.Error("učitavanje korespondenata", ex);
                return new List<Correspondent>();
            }
        }

        /// <summary>
        /// Skida PDF dokumenta u privremeni fajl i vraca putanju do njega.
        /// Potrebno jer WebView2 ne moze poslati Authorization zaglavlje.
        /// </summary>
        public async Task<string?> DownloadPreviewToTempAsync(int id, string title)
        {
            var client = ApiService.GetClient();
            try
            {
                var r = await client.GetAsync($"documents/{id}/preview/");

                if (!r.IsSuccessStatusCode)
                {
                    Notifier.HttpError($"pregled dokumenta #{id}", r.StatusCode);
                    return null;
                }

                var bytes = await r.Content.ReadAsByteArrayAsync();

                // Ocisti naslov od znakova koji nisu dozvoljeni u imenu fajla
                var safe = string.Join("_", (title ?? $"dokument_{id}")
                    .Split(System.IO.Path.GetInvalidFileNameChars()));
                if (safe.Length > 60) safe = safe.Substring(0, 60);

                var folder = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "PaperlessNGX");
                System.IO.Directory.CreateDirectory(folder);

                var path = System.IO.Path.Combine(folder, $"{id}_{safe}.pdf");
                await System.IO.File.WriteAllBytesAsync(path, bytes);

                return path;
            }
            catch (Exception ex)
            {
                Notifier.Error($"pregled dokumenta #{id}", ex);
                return null;
            }
        }

        public async Task<bool> SaveDocumentAsync(int id, string targetPath)
        {
            var client = ApiService.GetClient();
            try
            {
                var r = await client.GetAsync($"documents/{id}/download/");

                if (!r.IsSuccessStatusCode)
                {
                    Notifier.HttpError($"preuzimanje dokumenta #{id}", r.StatusCode);
                    return false;
                }

                var bytes = await r.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(targetPath, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Notifier.Error($"preuzimanje dokumenta #{id}", ex);
                return false;
            }
        }

        // ── URL-ovi ───────────────────────────────────────────────────────

        public string GetDownloadUrl(int id) =>
            $"{ApiService.Host}/api/documents/{id}/download/";

        public string GetPreviewUrl(int id) =>
            $"{ApiService.Host}/api/documents/{id}/preview/";

        public string GetThumbnailUrl(int id) =>
            $"{ApiService.Host}/api/documents/{id}/thumb/";
    }
}