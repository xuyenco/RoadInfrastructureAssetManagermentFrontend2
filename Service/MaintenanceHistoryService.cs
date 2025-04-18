using RoadInfrastructureAssetManagementFrontend2.Service;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class MaintenanceHistoryService : BaseService, IMaintenanceHistoryService
    {
        public MaintenanceHistoryService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<MaintenanceHistoryResponse>> GetAllMaintenanceHistories()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/MaintenanceHistories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MaintenanceHistoryResponse>>(content);
        }

        public async Task<List<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceHistories/AssetId/{id}"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MaintenanceHistoryResponse>>(content);
        }

        public async Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceHistories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve  with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
        }

        public async Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest request)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/MaintenanceHistories", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
        }

        public async Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest request)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/MaintenanceHistories/{id}", request));
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
                throw new HttpRequestException($"Failed to update maintenance history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
        }

        public async Task<bool> DeleteMaintenanceHistory(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceHistories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete maintenance history with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete maintenance history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
    }
}
