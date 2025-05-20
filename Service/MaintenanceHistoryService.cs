using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class MaintenanceHistoryService : BaseService, IMaintenanceHistoryService
    {
        private readonly ILogger<MaintenanceHistoryService> _logger;

        public MaintenanceHistoryService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<MaintenanceHistoryService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<MaintenanceHistoryResponse>> GetAllMaintenanceHistories()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all maintenance histories", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/MaintenanceHistories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance histories: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<MaintenanceHistoryResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} maintenance histories successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance histories for asset ID {AssetId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceHistories/AssetId/{id}"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance histories for asset ID {AssetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<MaintenanceHistoryResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} maintenance histories for asset ID {AssetId} successfully",
                username, role, result?.Count ?? 0, id);
            return result;
        }

        public class PagedMaintenanceHistoryResult
        {
            public List<MaintenanceHistoryResponse> maintenanceHistories { get; set; }
            public int totalCount { get; set; }
        }

        public async Task<PagedMaintenanceHistoryResult> GetPagedMaintenanceHistoryByAssetId(int id,int currentPage = 1,int pageSize = 10,string searchTerm = "",int searchField = 0)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation(
                "User {Username} (Role: {Role}) is retrieving paged maintenance histories for asset ID {AssetId} with parameters: Page={CurrentPage}, PageSize={PageSize}, SearchTerm='{SearchTerm}', SearchField={SearchField}",
                username, role, id, currentPage, pageSize, searchTerm, searchField);

            // Ensure searchTerm is not null
            searchTerm = searchTerm ?? "";

            // Build query string
            var query = new Dictionary<string, string>
        {
            { "currentPage", currentPage.ToString() },
            { "pageSize", pageSize.ToString() },
            { "searchTerm", searchTerm },
            { "searchField", searchField.ToString() }
        };
            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = $"api/MaintenanceHistories/AssetId/{id}/Paged?{queryString}";

            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync(requestUrl));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "User {Username} (Role: {Role}) failed to retrieve paged maintenance histories for asset ID {AssetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve paged maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedMaintenanceHistoryResult>(content);

            _logger.LogInformation(
                "User {Username} (Role: {Role}) retrieved {Count} maintenance histories for asset ID {AssetId} successfully (Page: {CurrentPage}, PageSize: {PageSize}, TotalCount: {TotalCount})",
                username, role, result?.maintenanceHistories?.Count ?? 0, id, currentPage, pageSize, result?.totalCount ?? 0);

            return result ?? new PagedMaintenanceHistoryResult { maintenanceHistories = new List<MaintenanceHistoryResponse>(), totalCount = 0 };
        }

        public async Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance history with ID {MaintenanceHistoryId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceHistories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance history with ID {MaintenanceHistoryId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance history with ID {MaintenanceHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved maintenance history with ID {MaintenanceHistoryId} successfully",
                username, role, id);
            return result;
        }

        public async Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating maintenance history", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending maintenance history data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/MaintenanceHistories", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create maintenance history: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create maintenance history: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create maintenance history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created maintenance history with ID {MaintenanceHistoryId} successfully",
                username, role, result?.maintenance_id);
            return result;
        }

        public async Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating maintenance history with ID {MaintenanceHistoryId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending maintenance history data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/MaintenanceHistories/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance history with ID {MaintenanceHistoryId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update maintenance history with ID {MaintenanceHistoryId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update maintenance history with ID {MaintenanceHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update maintenance history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceHistoryResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated maintenance history with ID {MaintenanceHistoryId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteMaintenanceHistory(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting maintenance history with ID {MaintenanceHistoryId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceHistories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance history with ID {MaintenanceHistoryId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance history with ID {MaintenanceHistoryId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete maintenance history with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance history with ID {MaintenanceHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete maintenance history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted maintenance history with ID {MaintenanceHistoryId} successfully",
                username, role, id);
            return true;
        }
    }
}