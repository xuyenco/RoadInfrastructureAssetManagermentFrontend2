using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class UsersService : BaseService, IUsersService
    {
        private readonly ILogger<UsersService> _logger;

        public UsersService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<UsersService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<UsersResponse>> GetAllUsersAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all users", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/users"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve users: Unauthorized access", username, role);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve users: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve users: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<UsersResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} users successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<UsersResponse?> GetUserByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving user with ID {UserId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/users/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no user with ID {UserId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve user with ID {UserId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve user with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UsersResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved user with ID {UserId} successfully",
                username, role, id);
            return result;
        }

        public async Task<UsersResponse?> CreateUserAsync(UsersRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating a new user with username {NewUsername}",
                username, role, request.username);
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.username ?? ""), "username");
            formData.Add(new StringContent(request.password_hash ?? ""), "password_hash");
            formData.Add(new StringContent(request.full_name ?? ""), "full_name");
            formData.Add(new StringContent(request.email ?? ""), "email");
            formData.Add(new StringContent(request.role ?? ""), "role");

            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included image for user creation: filename={FileName}, size={Size}",
                    username, role, request.image.FileName, request.image.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for creating user with username {NewUsername}",
                    username, role, request.username);
            }

            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for user creation: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/users", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create user with username {NewUsername}: Invalid request - {Error}",
                    username, role, request.username, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create user with username {NewUsername}: {StatusCode} - {Error}",
                    username, role, request.username, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create user: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UsersResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created user with ID {UserId} successfully",
                username, role, result?.user_id);
            return result;
        }

        public async Task<UsersResponse?> UpdateUserAsync(int id, UsersRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating user with ID {UserId}",
                username, role, id);
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.username ?? ""), "username");
            formData.Add(new StringContent(request.password_hash ?? ""), "password_hash");
            formData.Add(new StringContent(request.full_name ?? ""), "full_name");
            formData.Add(new StringContent(request.email ?? ""), "email");
            formData.Add(new StringContent(request.role ?? ""), "role");

            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included image for user update: filename={FileName}, size={Size}",
                    username, role, request.image.FileName, request.image.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for updating user with ID {UserId}",
                    username, role, id);
            }

            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for user update: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/users/{id}")
            {
                Content = formData
            };
            var response = await ExecuteWithRefreshAsync(() => _httpClient.SendAsync(requestMessage));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update user with ID {UserId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update user with ID {UserId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update user: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UsersResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated user with ID {UserId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting user with ID {UserId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/users/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) failed to delete user with ID {UserId}: {StatusCode}",
                    username, role, id, response.StatusCode);
                return false;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete user with ID {UserId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete user with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted user with ID {UserId} successfully",
                username, role, id);
            return true;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("User with username {Username} is attempting to login", request.Username);
            var client = CreateClientWithoutToken();
            var response = await client.PostAsJsonAsync("api/users/login", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Login attempt for username {Username} failed: Unauthorized", request.Username);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Login attempt for username {Username} failed: {StatusCode} - {Error}",
                    request.Username, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to login: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(content);
            _logger.LogInformation("User with username {Username} logged in successfully", request.Username);
            return result;
        }

        public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
        {
            _logger.LogInformation("Attempting to refresh token for user");
            var client = CreateClientWithoutToken();
            var response = await client.PostAsJsonAsync("api/users/refresh", refreshTokenRequest);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token refresh attempt failed: Unauthorized");
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh attempt failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to refresh token: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponse>(content);
            _logger.LogInformation("Token refreshed successfully");
            return result;
        }
    }
}