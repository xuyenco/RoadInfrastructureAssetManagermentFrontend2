using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class UsersService : BaseService, IUsersService
    {
        public UsersService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        // Get all users
        public async Task<List<UsersResponse>> GetAllUsersAsync()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/users"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expired or invalid. Please login againt.");
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UsersResponse>>(content);
        }

        // Get user by ID
        public async Task<UsersResponse?> GetUserByIdAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/users/{id}")) ;
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Create a new user
        public async Task<UsersResponse?> CreateUserAsync(UsersRequest request)
        {
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
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
            }
            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                Console.WriteLine($"{item.Headers.ContentDisposition.Name}: {value}");
            }

            var response = await ExecuteWithRefreshAsync(()=> _httpClient.PostAsync("api/users", formData)) ;
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Yêu cầu không hợp lệ: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Không thể tạo danh mục tài sản: {response.StatusCode} - {errorContent}");
            }
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Update an existing user
        public async Task<UsersResponse?> UpdateUserAsync(int id, UsersRequest request)
        {
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.username ?? ""), "username");
            formData.Add(new StringContent(request.password_hash ?? ""), "password_hash");
            formData.Add(new StringContent(request.full_name ?? ""), "full_name");
            formData.Add(new StringContent(request.email ?? ""), "email");
            formData.Add(new StringContent(request.role ?? ""), "role");

            // Thêm file ảnh nếu tồn tại
            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                Console.WriteLine($"Image included in update: filename={request.image.FileName}, size={request.image.Length}");
            }
            else
            {
                Console.WriteLine("No image provided for update.");
            }

            // Log dữ liệu gửi đi
            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                Console.WriteLine($"{item.Headers.ContentDisposition.Name}: {value}");
            }

            // Gửi yêu cầu PATCH
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/users/{id}")
            {
                Content = formData
            };
            var response = await ExecuteWithRefreshAsync(() =>  _httpClient.SendAsync(requestMessage));

            // Xử lý phản hồi
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update failed: {response.StatusCode} - {errorContent}");
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Không thể cập nhật user: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UsersResponse>(content);
        }

        // Delete a user
        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/users/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                return false;
            }
            response.EnsureSuccessStatusCode();

            return true;
        }

        // Login (dùng HttpClient không có token)
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var client = CreateClientWithoutToken(); // Tạo HttpClient không có token
            var response = await client.PostAsJsonAsync("api/users/login", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(content);
        }

        public async Task<LoginResponse?> RefreshTokenAsync (RefreshTokenRequest refreshTokenRequest)
        {
            var client = CreateClientWithoutToken();
            var response = await client.PostAsJsonAsync("api/users/refresh", refreshTokenRequest);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync ();
            return JsonSerializer.Deserialize<LoginResponse>(content);
        }
    }
}