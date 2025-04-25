using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class IncidentsService : BaseService, IIncidentsService
    {
        private readonly ILogger<IncidentsService> _logger;

        public IncidentsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<IncidentsService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<IncidentsResponse>> GetAllIncidentsAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all incidents", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/incidents"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incidents: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incidents: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentsResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incidents successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<IncidentsResponse?> GetIncidentByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident with ID {IncidentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/incidents/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident with ID {IncidentId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident with ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident with ID {IncidentId} successfully",
                username, role, id);
            return result;
        }

        public async Task<IncidentsResponse?> CreateIncidentAsync(IncidentsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating incident", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending incident data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/incidents", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create incident: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create incident: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create incident: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created incident with ID {IncidentId} successfully",
                username, role, result?.incident_id);
            return result;
        }

        public async Task<IncidentsResponse?> UpdateIncidentAsync(int id, IncidentsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating incident with ID {IncidentId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending incident data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/incidents/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident with ID {IncidentId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update incident with ID {IncidentId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update incident with ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update incident with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated incident with ID {IncidentId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteIncidentAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting incident with ID {IncidentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/incidents/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident with ID {IncidentId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete incident with ID {IncidentId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete incident with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete incident with ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete incident with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted incident with ID {IncidentId} successfully",
                username, role, id);
            return true;
        }
    }
}