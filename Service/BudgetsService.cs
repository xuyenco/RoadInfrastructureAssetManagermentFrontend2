using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class BudgetsService : BaseService, IBudgetsService
    {
        private readonly ILogger<BudgetsService> _logger;

        public BudgetsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<BudgetsService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<BudgetsResponse>> GetAllBudgetsAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all budgets", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/budgets"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve budgets: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve budgets: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<BudgetsResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} budgets successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<BudgetsResponse?> GetBudgetByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving budget with ID {BudgetId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/budgets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no budget with ID {BudgetId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve budget with ID {BudgetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve budget with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BudgetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved budget with ID {BudgetId} successfully",
                username, role, id);
            return result;
        }

        public async Task<BudgetsResponse?> CreateBudgetAsync(BudgetsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating budget", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending budget data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/budgets", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create budget: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create budget: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create budget: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BudgetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created budget with ID {BudgetId} successfully",
                username, role, result?.budget_id);
            return result;
        }

        public async Task<BudgetsResponse?> UpdateBudgetAsync(int id, BudgetsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating budget with ID {BudgetId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending budget data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/budgets/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no budget with ID {BudgetId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update budget with ID {BudgetId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update budget with ID {BudgetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update budget with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<BudgetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated budget with ID {BudgetId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteBudgetAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting budget with ID {BudgetId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/budgets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no budget with ID {BudgetId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete budget with ID {BudgetId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete budget with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete budget with ID {BudgetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete budget with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted budget with ID {BudgetId} successfully",
                username, role, id);
            return true;
        }
    }
}