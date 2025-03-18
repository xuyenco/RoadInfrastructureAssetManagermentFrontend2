using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class IncidentsService :IIncidentsService
    {
        private readonly HttpClient _httpClient;

        public IncidentsService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all incidents
        public async Task<List<IncidentsResponse>> GetAllIncidentsAsync()
        {
            var response = await _httpClient.GetAsync("api/incidents");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentsResponse>>(content);
        }

        // Get asset by ID
        public async Task<IncidentsResponse?> GetIncidentByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/incidents/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Incident not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentsResponse>(content);
        }

        // Create a new asset
        public async Task<IncidentsResponse?> CreateIncidentAsync(IncidentsRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/incidents", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentsResponse>(content);
        }

        // Update an existing asset
        public async Task<IncidentsResponse?> UpdateIncidentAsync(int id,IncidentsRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/incidents/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Incident not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentsResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteIncidentAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/incidents/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Incident not found
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
