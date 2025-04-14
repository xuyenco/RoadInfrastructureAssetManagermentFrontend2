using RoadInfrastructureAssetManagementFrontend.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public abstract class BaseService
    {
        protected readonly HttpClient _httpClient;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public BaseService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
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
            var response = await client.PostAsJsonAsync("api/users/refresh", refreshTokenRequest);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Refresh token failed with 401");
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(content);
        }

        protected async Task<HttpResponseMessage> ExecuteWithRefreshAsync(Func<Task<HttpResponseMessage>> apiCall)
        {
            var response = await apiCall();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Refresh token time!!!!!!");
                var refreshToken = _httpContextAccessor.HttpContext.Session.GetString("RefreshToken");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine("No refresh token available");
                    throw new UnauthorizedAccessException("No refresh token available.");
                }
                var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
                if (refreshResponse == null)
                {
                    Console.WriteLine("Refresh token failed or expired");
                    throw new UnauthorizedAccessException("Refresh token failed or expired.");
                }
                _httpContextAccessor.HttpContext.Session.SetString("AccessToken", refreshResponse.accessToken);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshResponse.accessToken);
                Console.WriteLine($"New AccessToken: {refreshResponse.accessToken}");
                response = await apiCall(); // Thử lại yêu cầu ban đầu
            }
            return response;
        }
    }
}