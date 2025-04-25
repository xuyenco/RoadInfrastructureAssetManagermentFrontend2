using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<List<TaskStatusDistribution>> GetTaskStatusDistributions()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving task status distribution report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/TaskStatusDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve task status distribution report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve task status distribution report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TaskStatusDistribution>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved task status distribution report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<IncidentTypeDistribution>> GetIncidentTypeDistributions()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident type distribution report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/IncidentTypeDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident type distribution report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident type distribution report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentTypeDistribution>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident type distribution report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<IncidentsOverTime>> GetIncidentsOverTime()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incidents over time report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/IncidentsOverTime"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incidents over time report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incidents over time report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentsOverTime>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incidents over time report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<BudgetAndCost>> GetBudgetAndCosts()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving budget and costs report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/BudgetAndCosts"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve budget and costs report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve budget and costs report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<BudgetAndCost>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved budget and costs report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<AssetDistributionByCategory>> GetAssetDistributionByCategories()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset distribution by categories report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/AssetDistributionByCategories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset distribution by categories report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve asset distribution by categories report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<AssetDistributionByCategory>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved asset distribution by categories report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<List<AssetDistributedByCondition>> GetAssetDistributedByCondition()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset distribution by condition report", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/Report/AssetDistributedByCondition"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset distribution by condition report: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve asset distribution by condition report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<AssetDistributedByCondition>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved asset distribution by condition report with {Count} items successfully",
                username, role, result?.Count ?? 0);
            return result;
        }
    }
}