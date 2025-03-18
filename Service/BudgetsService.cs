using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class BudgetsService : IBudgetsService
    {
        private readonly HttpClient _httpClient;

        public BudgetsService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all budgets
        public async Task<List<BudgetsResponse>> GetAllBudgetsAsync()
        {
            var response = await _httpClient.GetAsync("api/budgets");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<BudgetsResponse>>(content);
        }

        // Get asset by ID
        public async Task<BudgetsResponse?> GetBudgetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/budgets/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Budget not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BudgetsResponse>(content);
        }

        // Create a new asset
        public async Task<BudgetsResponse?> CreateBudgetAsync(BudgetsRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/budgets", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BudgetsResponse>(content);
        }

        // Update an existing asset
        public async Task<BudgetsResponse?> UpdateBudgetAsync(int id, BudgetsRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/budgets/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Budget not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BudgetsResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteBudgetAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/budgets/{id}");
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
