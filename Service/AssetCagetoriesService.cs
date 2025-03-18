using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class AssetCagetoriesService : IAssetCagetoriesService
    {
        private readonly HttpClient _httpClient;

        public AssetCagetoriesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all assetcagetories
        public async Task<List<AssetCagetoriesResponse>> GetAllAssetCagetoriesAsync()
        {
            var response = await _httpClient.GetAsync("api/assetcagetories");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AssetCagetoriesResponse>>(content);
        }

        // Get asset by ID
        public async Task<AssetCagetoriesResponse?> GetAssetCagetoriesByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/assetcagetories/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Asset not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        // Create a new asset
        public async Task<AssetCagetoriesResponse?> CreateAssetCagetoriesAsync(AssetCagetoriesRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/assetcagetories", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        // Update an existing asset
        public async Task<AssetCagetoriesResponse?> UpdateAssetCagetoriesAsync(int id, AssetCagetoriesRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/assetcagetories/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Asset not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteAssetCagetoriesAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/assetcagetories/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Asset not found
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
