using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public abstract class BaseService
    {
        protected readonly HttpClient _httpClient;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ILogger<BaseService> _logger;

        public BaseService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<BaseService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _httpClient = _httpClientFactory.CreateClient("ApiClient");

            var token = httpContextAccessor.HttpContext?.Session.GetString("AccessToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        protected HttpClient CreateClientWithoutToken()
        {
            return _httpClientFactory.CreateClient("ApiClient");
        }

        protected async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest refreshTokenRequest)
        {
            var client = CreateClientWithoutToken();
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to refresh token", username, role);
            var response = await client.PostAsJsonAsync("api/users/refresh", refreshTokenRequest);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) failed to refresh token: Unauthorized", username, role);
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("User {Username} (Role: {Role}) refreshed token successfully", username, role);
            return JsonSerializer.Deserialize<LoginResponse>(content);
        }

        protected async Task<HttpResponseMessage> ExecuteWithRefreshAsync(Func<Task<HttpResponseMessage>> apiCall)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            var response = await apiCall();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) received Unauthorized response, attempting to refresh token", username, role);
                var refreshToken = _httpContextAccessor.HttpContext?.Session.GetString("RefreshToken");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogError("User {Username} (Role: {Role}) has no refresh token available", username, role);
                    throw new UnauthorizedAccessException("No refresh token available.");
                }
                var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
                if (refreshResponse == null)
                {
                    _logger.LogError("User {Username} (Role: {Role}) failed to refresh token: Expired or invalid", username, role);
                    throw new UnauthorizedAccessException("Refresh token failed or expired.");
                }
                _httpContextAccessor.HttpContext?.Session.SetString("AccessToken", refreshResponse.accessToken);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshResponse.accessToken);
                _logger.LogInformation("User {Username} (Role: {Role}) obtained new access token", username, role);
                response = await apiCall();
            }
            return response;
        }
    }
}