using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;

namespace BlazorClient.Services;

public class ServerApiService
{
    private readonly HttpClient _httpClient;
    private const string WebUsername = "webuser";
    private const string WebPassword = "webpass";

    public ServerApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Set up Basic Auth header for all requests
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{WebUsername}:{WebPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<int> GetClientCountAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CountResponse>("http://localhost:5000/api/clientcount");
            return response?.Count ?? 0;
        }
        catch
        {
            return -1; // Error indicator
        }
    }

    public async Task<int> GetFileCountAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CountResponse>("http://localhost:5000/api/filecount");
            return response?.Count ?? 0;
        }
        catch
        {
            return -1; // Error indicator
        }
    }

    private class CountResponse
    {
        public int Count { get; set; }
    }
}

