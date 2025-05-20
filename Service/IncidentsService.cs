using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Text.Json;

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

        public async Task<(List<IncidentsResponse> Incidents, int TotalCount)> GetIncidentsAsync(int page, int pageSize, string searchTerm, int searchField)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incidents with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, page, pageSize, searchTerm, searchField);

            // Xây dựng query string cho API
            var query = new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "pageSize", pageSize.ToString() },
                { "searchTerm", searchTerm ?? "" },
                { "searchField", searchField.ToString() }
            };

            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = $"api/incidents/paged?{queryString}";

            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync(requestUrl));

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incidents: Unauthorized access", username, role);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incidents: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incidents: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentsPaginationResponse>(content);

            if (result?.incidents == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) received empty incidents list for Page: {Page}", username, role, page);
                return (new List<IncidentsResponse>(), 0);
            }

            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incidents with total count {TotalCount} for Page: {Page}",
                username, role, result.incidents.Count, result.totalCount, page);

            return (result.incidents, result.totalCount);
        }

        public class IncidentsPaginationResponse
        {
            public List<IncidentsResponse> incidents { get; set; }
            public int totalCount { get; set; }
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