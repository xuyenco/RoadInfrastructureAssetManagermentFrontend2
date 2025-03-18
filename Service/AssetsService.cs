using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class AssetsService : IAssetsService
    {
        private readonly HttpClient _httpClient;

        public AssetsService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); 
        }

        // Get all assets
        public async Task<List<AssetsResponse>> GetAllAssetsAsync()
        {
            var response = await _httpClient.GetAsync("api/assets");
            var content = await response.Content.ReadAsStringAsync();
            var assets = JsonSerializer.Deserialize<List<AssetsResponse>>(content);
            return assets;
        }

        // Get asset by ID
        public async Task<AssetsResponse?> GetAssetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/assets/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Asset not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        // Create a new asset
        public async Task<AssetsResponse?> CreateAssetAsync(AssetsRequest request)
        {
            //Console.WriteLine($"Request data: {JsonSerializer.Serialize(request)}");
            var response = await _httpClient.PostAsJsonAsync("api/assets", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        // Update an existing asset
        public async Task<AssetsResponse?> UpdateAssetAsync(int id,AssetsRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/assets/{id}", request);
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
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteAssetAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/assets/{id}");
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
