using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class TasksService : BaseService, ITasksService
    {
        private readonly ILogger<TasksService> _logger;

        public TasksService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<TasksService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<TasksResponse>> GetAllTasksAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all tasks", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/tasks"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve tasks: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve tasks: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TasksResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} tasks successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<TasksResponse?> GetTaskByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving task with ID {TaskId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/tasks/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve task with ID {TaskId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve task with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TasksResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved task with ID {TaskId} successfully",
                username, role, id);
            return result;
        }

        public async Task<TasksResponse?> CreateTaskAsync(TasksRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating task", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) sending task data: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsJsonAsync("api/tasks", request));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create task: Invalid request - {Error}",
                    username, role, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create task: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create task: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TasksResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created task with ID {TaskId} successfully",
                username, role, result?.task_id);
            return result;
        }

        public async Task<TasksResponse?> UpdateTaskAsync(int id, TasksRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating task with ID {TaskId}",
                username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) sending task data for update: {Request}",
                username, role, JsonSerializer.Serialize(request));
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsJsonAsync($"api/tasks/{id}", request));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId} for update",
                    username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update task with ID {TaskId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update task with ID {TaskId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update task with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TasksResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated task with ID {TaskId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting task with ID {TaskId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/tasks/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete task with ID {TaskId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete task with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete task with ID {TaskId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete task with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted task with ID {TaskId} successfully",
                username, role, id);
            return true;
        }
    }
}