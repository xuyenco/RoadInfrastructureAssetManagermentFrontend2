using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class IncidentHistoriesService : BaseService, IIncidentHistoriesService
    {
        private readonly ILogger<IncidentHistoriesService> _logger;

        public IncidentHistoriesService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<IncidentHistoriesService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<IncidentHistoriesResponse>> GetAllIncidentHistoriesAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all incident histories", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/incidentHistory"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident histories: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident histories: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentHistoriesResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incident histories successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<IncidentHistoriesResponse>> GetIncidentHistoriesByIncidentId(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident histories for incident ID {IncidentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/incidentHistory/incidentid/{id}"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident histories for incident ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident histories: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentHistoriesResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incident histories for incident ID {IncidentId} successfully",
                username, role, result?.Count ?? 0, id);
            return result;
        }

        public async Task<IncidentHistoriesResponse?> GetIncidentHistoryByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident history with ID {IncidentHistoryId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/incidentHistory/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident history with ID {IncidentHistoryId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident history with ID {IncidentHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident history with ID {IncidentHistoryId} successfully",
                username, role, id);
            return result;
        }

        public async Task<IncidentHistoriesResponse?> CreateIncidentHistoryAsync(IncidentHistoriesRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating incident history", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending incident history data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/incidentHistory", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create incident history: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create incident history: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create incident history: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created incident history with ID {IncidentHistoryId} successfully",
                username, role, result?.incident_id);
            return result;
        }

        public async Task<IncidentHistoriesResponse?> UpdateIncidentHistoryAsync(int id, IncidentHistoriesRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating incident history with ID {IncidentHistoryId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending incident history data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/incidentHistory/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident history with ID {IncidentHistoryId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update incident history with ID {IncidentHistoryId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update incident history with ID {IncidentHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update incident history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentHistoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated incident history with ID {IncidentHistoryId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteIncidentHistoryAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting incident history with ID {IncidentHistoryId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/incidentHistory/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident history with ID {IncidentHistoryId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete incident history with ID {IncidentHistoryId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete incident history with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete incident history with ID {IncidentHistoryId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete incident history with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted incident history with ID {IncidentHistoryId} successfully",
                username, role, id);
            return true;
        }
    }
}