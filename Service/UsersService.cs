using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class UsersService : IUsersService
    {
        private readonly HttpClient _httpClient;

        public UsersService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient"); // Lấy HttpClient có BaseAddress đã cấu hình
        }

        // Get all users
        public async Task<List<UsersResponse>> GetAllUsersAsync()
        {
            var response = await _httpClient.GetAsync("api/users");
            response.EnsureSuccessStatusCode(); // Throw if not successful

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UsersResponse>>(content);
        }

        // Get asset by ID
        public async Task<UsersResponse?> GetUserByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/users/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // User not found
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Create a new asset
        public async Task<UsersResponse?> CreateUserAsync(UsersRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users", request);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Update an existing asset
        public async Task<UsersResponse?> UpdateUserAsync(int id, UsersRequest request)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/users/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // User not found
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return null; // Bad request
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Delete an asset
        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/users/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // User not found
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
