using HCI_2025_Project_Template.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HCI_2025_Project_Template.Services
{
    public class TagService
    {
        private readonly HttpClient _client;

        public TagService()
        {
            _client = ApiService.GetClient();
        }

        // GET /api/tags/
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            var allTags = new List<Tag>();
            try
            {
                string? url = "tags/?page_size=100";
                while (!string.IsNullOrEmpty(url))
                {
                    var r = await _client.GetAsync(url);
                    if (!r.IsSuccessStatusCode) break;

                    var resp = await r.Content.ReadFromJsonAsync<TagResponse>();
                    if (resp == null) break;

                    allTags.AddRange(resp.Results);

                    // Sljedeća stranica
                    if (!string.IsNullOrEmpty(resp.Next))
                    {
                        var nextUri = new Uri(resp.Next);
                        url = nextUri.PathAndQuery.TrimStart('/').Replace("api/", "");
                    }
                    else
                    {
                        url = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllTags error: {ex.Message}");
            }
            return allTags;
        }

        // POST /api/tags/
        public async Task<Tag?> CreateTagAsync(string name, string colorHex)
        {
            try
            {
                var payload = new
                {
                    name = name,
                    color = colorHex
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var r = await _client.PostAsync("tags/", content);

                if (r.IsSuccessStatusCode)
                    return await r.Content.ReadFromJsonAsync<Tag>();

                var err = await r.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CreateTag failed: {r.StatusCode} — {err}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateTag error: {ex.Message}");
            }
            return null;
        }

        // PATCH /api/tags/{id}/
        public async Task<Tag?> UpdateTagAsync(int id, string name, string colorHex)
        {
            try
            {
                var payload = new
                {
                    name = name,
                    color = colorHex
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var r = await _client.PatchAsync($"tags/{id}/", content);

                if (r.IsSuccessStatusCode)
                    return await r.Content.ReadFromJsonAsync<Tag>();

                var err = await r.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"UpdateTag failed: {r.StatusCode} — {err}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTag error: {ex.Message}");
            }
            return null;
        }

        // DELETE /api/tags/{id}/
        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var r = await _client.DeleteAsync($"tags/{id}/");
                return r.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteTag error: {ex.Message}");
                return false;
            }
        }
    }
}