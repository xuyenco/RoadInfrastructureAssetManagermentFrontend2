using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class TasksService :ITasksService
    {
        private readonly HttpClient _httpClient;

        public TasksService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all tasks
        public async Task<List<TasksResponse>> GetAllTasksAsync()
        {
            var response = await _httpClient.GetAsync("api/tasks");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TasksResponse>>(content);
        }

        // Get asset by ID
        public async Task<TasksResponse?> GetTaskByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/tasks/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Task not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TasksResponse>(content);
        }

        // Create a new asset
        public async Task<TasksResponse?> CreateTaskAsync(TasksRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/tasks", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TasksResponse>(content);
        }

        // Update an existing asset
        public async Task<TasksResponse?> UpdateTaskAsync(int id, TasksRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/tasks/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Task not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TasksResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteTaskAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/tasks/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Task not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return false; // Bad request
            }
            response.EnsureSuccessStatusCode();

            return true; // Successfully deleted
        }
    }
}
