using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class CostsService : BaseService, ICostsService
    {
        private readonly ILogger<CostsService> _logger;

        public CostsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<CostsService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<CostsResponse>> GetAllCostsAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all costs", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/costs"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve costs: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve costs: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<CostsResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} costs successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<CostsResponse?> GetCostByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving cost with ID {CostId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/costs/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no cost with ID {CostId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve cost with ID {CostId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CostsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved cost with ID {CostId} successfully",
                username, role, id);
            return result;
        }

        public async Task<CostsResponse?> CreateCostAsync(CostsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating cost", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending cost data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/costs", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create cost: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create cost: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create cost: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CostsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created cost with ID {CostId} successfully",
                username, role, result?.cost_id);
            return result;
        }

        public async Task<CostsResponse?> UpdateCostAsync(int id, CostsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating cost with ID {CostId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending cost data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/costs/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no cost with ID {CostId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update cost with ID {CostId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update cost with ID {CostId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CostsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated cost with ID {CostId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteCostAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting cost with ID {CostId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/costs/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no cost with ID {CostId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete cost with ID {CostId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete cost with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete cost with ID {CostId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete cost with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted cost with ID {CostId} successfully",
                username, role, id);
            return true;
        }
    }
}