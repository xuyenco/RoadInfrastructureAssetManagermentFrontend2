using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class IncidentHistoriesService : IIncidentHistoriesService
    {
        private readonly HttpClient _httpClient;

        public IncidentHistoriesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all incidentHistories
        public async Task<List<IncidentHistoriesResponse>> GetAllIncidentHistoriesAsync()
        {
            var response = await _httpClient.GetAsync("api/incidentHistory");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentHistoriesResponse>>(content);
        }

        // Get asset by ID
        public async Task<IncidentHistoriesResponse?> GetIncidentHistoryByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/incidentHistory/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // IncidentHistory not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
        }

        // Create a new asset
        public async Task<IncidentHistoriesResponse?> CreateIncidentHistoryAsync(IncidentHistoriesRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/incidentHistory", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
        }

        // Update an existing asset
        public async Task<IncidentHistoriesResponse?> UpdateIncidentHistoryAsync(int id, IncidentHistoriesRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/incidentHistory/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // IncidentHistory not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteIncidentHistoryAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/incidentHistory/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // IncidentHistory not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return false; // Bad request
            }
            response.EnsureSuccessStatusCode();

            return true; // Successfully deleted
        }
    }
}
