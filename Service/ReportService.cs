using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class ReportService : BaseService, IReportService
    {
        private readonly ILogger<ReportService> _logger;

        public ReportService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<ReportService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<AssetStatusReport>> GetAssetDistributedByCondition()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset status report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/AssetDistributedByCondition"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset status report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve asset status report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<List<AssetStatusReport>>(content, options);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved asset status report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<IncidentDistributionReport>> GetIncidentTypeDistributions()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident distribution report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/IncidentTypeDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident distribution report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident distribution report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<List<IncidentDistributionReport>>(content, options);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident distribution report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<TaskPerformanceReport>> GetTaskStatusDistributions()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving task performance report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/TaskStatusDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve task performance report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve task performance report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TaskPerformanceReport>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved task performance report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<IncidentTaskTrendReport>> GetIncidentsOverTime()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident and task trend report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/IncidentsOverTime"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident and task trend report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident and task trend report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentTaskTrendReport>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident and task trend report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<MaintenanceFrequencyReport>> GetMaintenanceFrequency()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance frequency report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/MaintenanceFrequency"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance frequency report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance frequency report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<MaintenanceFrequencyReport>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved maintenance frequency report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }
    }
}