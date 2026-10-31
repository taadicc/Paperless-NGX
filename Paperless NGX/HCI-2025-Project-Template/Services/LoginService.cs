using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace HCI_2025_Project_Template.Services
{
    public enum LoginStatus { Success, InvalidCredentials, NetworkError, ServerError }

    public record LoginResult(LoginStatus Status, string? Message = null);

    public class LoginService
    {
        public async Task<LoginResult> Authenticate(string username, string password)
        {
            var client = ApiService.GetClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        });

            try
            {
                var response = await client.PostAsync("token/", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                    if (!string.IsNullOrEmpty(result?.Token))
                    {
                        ApiService.SetToken(result.Token);
                        return new LoginResult(LoginStatus.Success);
                    }
                    return new LoginResult(LoginStatus.ServerError, "Server nije vratio token.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new LoginResult(LoginStatus.InvalidCredentials);

                return new LoginResult(LoginStatus.ServerError, $"Server je vratio {(int)response.StatusCode}.");
            }
            catch (HttpRequestException ex)
            {
                return new LoginResult(LoginStatus.NetworkError, ex.Message);
            }
            catch (TaskCanceledException)
            {
                return new LoginResult(LoginStatus.NetworkError, "Isteklo je vrijeme čekanja odgovora.");
            }
        }
    }

    // Pomoćna klasa za hvatanje tokena iz JSON-a
    public class TokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}