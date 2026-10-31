using HCI_2025_Project_Template.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HCI_2025_Project_Template.Services
{
    public class SearchService
    {
        private readonly System.Net.Http.HttpClient _client;

        public SearchService()
        {
            _client = ApiService.GetClient();
        }

        // GET /api/search/?query=...
        // Vraća dokumente sa relevancy score-om
        public async Task<SearchResponse> SearchAsync(string query, int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return new SearchResponse();

                // Ispravan endpoint — nema page parametra za global search
                var url = $"search/?query={Uri.EscapeDataString(query)}";

                var r = await _client.GetAsync(url);
                if (!r.IsSuccessStatusCode)
                    return new SearchResponse();

                return await r.Content.ReadFromJsonAsync<SearchResponse>()
                       ?? new SearchResponse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                return new SearchResponse();
            }
        }

        // GET /api/search/autocomplete/?term=...
        // Vraća listu prijedloga za autocomplete
        public async Task<List<string>> AutocompleteAsync(string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return new List<string>();

                var url = $"search/autocomplete/?term={Uri.EscapeDataString(term)}";
                var r = await _client.GetAsync(url);

                if (!r.IsSuccessStatusCode) return new List<string>();

                // API vraća JSON array stringova
                var results = await r.Content
                    .ReadFromJsonAsync<List<string>>();
                return results ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Autocomplete error: {ex.Message}");
                return new List<string>();
            }
        }
    }
}