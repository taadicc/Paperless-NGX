using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HCI_2025_Project_Template.Services
{
    public class ApiService
    {
        public const string Host = "http://paperless-etf.duckdns.org:8000";
        public const string BaseUrl = Host + "/api/";

        private static HttpClient? _httpClient;

        public static HttpClient GetClient()
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(BaseUrl),
                    Timeout = TimeSpan.FromSeconds(15)
                };
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
            return _httpClient;
        }

        public static void SetToken(string token)
        {
            var client = GetClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {token}");
        }
    }
}