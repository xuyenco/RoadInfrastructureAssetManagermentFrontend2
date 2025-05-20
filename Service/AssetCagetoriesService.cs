using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class AssetCagetoriesService : BaseService, IAssetCagetoriesService
    {
        private readonly ILogger<AssetCagetoriesService> _logger; // Thêm logger

        public AssetCagetoriesService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<AssetCagetoriesService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<AssetCagetoriesResponse>> GetAllAssetCagetoriesAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all asset categories", username, role); // Log gọi API
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/assetCagetories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset categories: {StatusCode} - {Error}", username, role, response.StatusCode, errorContent); // Log lỗi
                throw new HttpRequestException($"Failed to retrieve asset categories: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<AssetCagetoriesResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} asset categories successfully", username, role, result?.Count ?? 0); // Log thành công
            return result;
        }

        public async Task<AssetCagetoriesResponse?> GetAssetCagetoriesByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset category with ID {CategoryId}", username, role, id); // Log gọi API
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/assetCagetories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId}", username, role, id); // Log không tìm thấy
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset category with ID {CategoryId}: {StatusCode} - {Error}", username, role, id, response.StatusCode, errorContent); // Log lỗi
                throw new HttpRequestException($"Failed to retrieve asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved asset category with ID {CategoryId} successfully", username, role, id); // Log thành công
            return result;
        }

        public async Task<AssetCagetoriesResponse?> CreateAssetCagetoriesAsync(AssetCagetoriesRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating asset category with name {CategoryName}", username, role, request.category_name); // Log gọi API
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.category_name ?? ""), "category_name");
            formData.Add(new StringContent(request.geometry_type ?? ""), "geometry_type");
            formData.Add(new StringContent(request.attribute_schema ?? ""), "attribute_schema");

            // Xử lý file ảnh
            if (request.sample_image != null && request.sample_image.Length > 0)
            {
                var fileContent = new StreamContent(request.sample_image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.sample_image.ContentType);
                formData.Add(fileContent, "sample_image", request.sample_image.FileName);
            }

            if (request.icon != null && request.icon.Length > 0)
            {
                var fileContent = new StreamContent(request.icon.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.icon.ContentType);
                formData.Add(fileContent, "icon", request.icon.FileName);
            }

            // Log dữ liệu gửi lên (thay Console.WriteLine)
            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data: {Key} = {Value}", username, role, item.Headers.ContentDisposition.Name, value); // Log dữ liệu form
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/AssetCagetories", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create asset category: Invalid request - {Error}", username, role, errorContent); // Log lỗi 400
                throw new ArgumentException($"Yêu cầu không hợp lệ: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create asset category: {StatusCode} - {Error}", username, role, response.StatusCode, errorContent); // Log lỗi
                throw new HttpRequestException($"Không thể tạo danh mục tài sản: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created asset category with ID {CategoryId} successfully", username, role, result?.category_id); // Log thành công
            return result;
        }

        public async Task<AssetCagetoriesResponse?> UpdateAssetCagetoriesAsync(int id, AssetCagetoriesRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating asset category with ID {CategoryId}", username, role, id); // Log gọi API
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.category_name ?? ""), "category_name");
            formData.Add(new StringContent(request.geometry_type ?? ""), "geometry_type");
            formData.Add(new StringContent(request.attribute_schema ?? ""), "attribute_schema");

            if (request.sample_image != null && request.sample_image.Length > 0)
            {
                var fileContent = new StreamContent(request.sample_image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.sample_image.ContentType);
                formData.Add(fileContent, "sample_image", request.sample_image.FileName);
            }
            if (request.icon != null && request.icon.Length > 0)
            {
                var fileContent = new StreamContent(request.icon.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.icon.ContentType);
                formData.Add(fileContent, "icon", request.icon.FileName);
            }

            // Log dữ liệu gửi lên (thay Console.WriteLine)
            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data: {Key} = {Value}", username, role, item.Headers.ContentDisposition.Name, value); // Log dữ liệu form
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsync($"api/assetCagetories/{id}", formData));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId} for update", username, role, id); // Log không tìm thấy
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update asset category with ID {CategoryId}: Invalid request - {Error}", username, role, id, errorContent); // Log lỗi 400
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update asset category with ID {CategoryId}: {StatusCode} - {Error}", username, role, id, response.StatusCode, errorContent); // Log lỗi
                throw new HttpRequestException($"Failed to update asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated asset category with ID {CategoryId} successfully", username, role, id); // Log thành công
            return result;
        }

        public async Task<bool> DeleteAssetCagetoriesAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting asset category with ID {CategoryId}", username, role, id); // Log gọi API
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/assetCagetories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId} for deletion", username, role, id); // Log không tìm thấy
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete asset category with ID {CategoryId}: {Error}", username, role, id, errorContent); // Log lỗi
                throw new InvalidOperationException($"Failed to delete asset cagetory with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete asset category with ID {CategoryId}: {StatusCode} - {Error}", username, role, id, response.StatusCode, errorContent); // Log lỗi
                throw new HttpRequestException($"Failed to delete asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted asset category with ID {CategoryId} successfully", username, role, id); // Log thành công
            return true;
        }
    }
}