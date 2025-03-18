using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class CostsService : ICostsService
    {
        private readonly HttpClient _httpClient;

        public CostsService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all costs
        public async Task<List<CostsResponse>> GetAllCostsAsync()
        {
            var response = await _httpClient.GetAsync("api/costs");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CostsResponse>>(content);
        }

        // Get asset by ID
        public async Task<CostsResponse?> GetCostByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/costs/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Cost not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        // Create a new asset
        public async Task<CostsResponse?> CreateCostAsync(CostsRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/costs", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        // Update an existing asset
        public async Task<CostsResponse?> UpdateCostAsync(int id,CostsRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/costs/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Cost not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteCostAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/costs/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Cost not found
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
