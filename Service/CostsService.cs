using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class CostsService : BaseService,ICostsService
    {
        public CostsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<CostsResponse>> GetAllCostsAsync()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/costs"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve costs: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CostsResponse>>(content);
        }

        public async Task<CostsResponse?> GetCostByIdAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/costs/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        public async Task<CostsResponse?> CreateCostAsync(CostsRequest request)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/costs", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create cost: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        public async Task<CostsResponse?> UpdateCostAsync(int id, CostsRequest request)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/costs/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to update cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CostsResponse>(content);
        }

        public async Task<bool> DeleteCostAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/costs/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete cost with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
    }
}