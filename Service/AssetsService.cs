using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class AssetsService : BaseService, IAssetsService
    {
        private readonly ILogger<AssetsService> _logger;

        public AssetsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<AssetsService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<AssetsResponse>> GetAllAssetsAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all assets", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/assets"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve assets: {StatusCode} - {Error}", username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve assets: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<AssetsResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} assets successfully", username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<(List<AssetsResponse> Assets, int TotalCount)> GetAssetsAsync(int page, int pageSize, string searchTerm, int searchField)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving assets with pagination - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, page, pageSize, searchTerm, searchField);

            // Xây dựng query string cho API
            var query = new Dictionary<string, string>
    {
        { "page", page.ToString() },
        { "pageSize", pageSize.ToString() },
        { "searchTerm", searchTerm ?? "" },
        { "searchField", searchField.ToString() }
    };
            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var requestUrl = $"api/assets/paged?{queryString}";

            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync(requestUrl));

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve assets: Unauthorized access", username, role);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve assets: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve assets: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetsPaginationResponse>(content);

            if (result?.assets == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) received empty assets list for Page: {Page}", username, role, page);
                return (new List<AssetsResponse>(), 0);
            }

            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} assets with total count {TotalCount} for Page: {Page}",
                username, role, result.assets.Count, result.totalCount, page);

            return (result.assets, result.totalCount);
        }        // Lớp để ánh xạ phản hồi JSON từ API
        public class AssetsPaginationResponse
        {
            public List<AssetsResponse> assets { get; set; }
            public int totalCount { get; set; }
        }

        public async Task<AssetsResponse?> GetAssetByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset with ID {AssetId}", username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/assets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId}", username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve asset with ID {AssetId}: {StatusCode} - {Error}", username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve asset with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved asset with ID {AssetId} successfully", username, role, id);
            return result;
        }

        public async Task<AssetsResponse?> CreateAssetAsync(AssetsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating asset with name {AssetName} and category ID {CategoryId}",
                username, role, request.asset_name, request.category_id);
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.category_id.ToString()), "category_id");
            formData.Add(new StringContent(request.asset_name ?? ""), "asset_name");
            formData.Add(new StringContent(request.asset_code ?? ""), "asset_code");
            formData.Add(new StringContent(request.address ?? ""), "address");
            formData.Add(new StringContent(JsonSerializer.Serialize(request.geometry)), "geometry");
            if (request.construction_year.HasValue)
                formData.Add(new StringContent(request.construction_year.Value.ToString("o")), "construction_year");
            if (request.operation_year.HasValue)
                formData.Add(new StringContent(request.operation_year.Value.ToString("o")), "operation_year");
            if (request.land_area.HasValue)
                formData.Add(new StringContent(request.land_area.Value.ToString()), "land_area");
            if (request.floor_area.HasValue)
                formData.Add(new StringContent(request.floor_area.Value.ToString()), "floor_area");
            if (request.original_value.HasValue)
                formData.Add(new StringContent(request.original_value.Value.ToString()), "original_value");
            if (request.remaining_value.HasValue)
                formData.Add(new StringContent(request.remaining_value.Value.ToString()), "remaining_value");
            formData.Add(new StringContent(request.asset_status ?? ""), "asset_status");
            formData.Add(new StringContent(request.installation_unit ?? ""), "installation_unit");
            formData.Add(new StringContent(request.management_unit ?? ""), "management_unit");
            formData.Add(new StringContent(request.custom_attributes ?? ""), "custom_attributes");

            // Xử lý file ảnh
            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for asset creation with name {AssetName}",
                    username, role, request.asset_name);
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
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for asset creation: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/assets", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create asset with name {AssetName}: Invalid request - {Error}",
                    username, role, request.asset_name, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create asset with name {AssetName}: {StatusCode} - {Error}",
                    username, role, request.asset_name, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create asset: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created asset with ID {AssetId} successfully",
                username, role, result?.asset_id);
            return result;
        }

        public async Task<AssetsResponse?> UpdateAssetAsync(int id, AssetsRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating asset with ID {AssetId} and name {AssetName}",
                username, role, id, request.asset_name);
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.category_id.ToString()), "category_id");
            formData.Add(new StringContent(request.asset_name ?? ""), "asset_name");
            formData.Add(new StringContent(request.asset_code ?? ""), "asset_code");
            formData.Add(new StringContent(request.address ?? ""), "address");
            formData.Add(new StringContent(JsonSerializer.Serialize(request.geometry)), "geometry");
            if (request.construction_year.HasValue)
                formData.Add(new StringContent(request.construction_year.Value.ToString("o")), "construction_year");
            if (request.operation_year.HasValue)
                formData.Add(new StringContent(request.operation_year.Value.ToString("o")), "operation_year");
            if (request.land_area.HasValue)
                formData.Add(new StringContent(request.land_area.Value.ToString()), "land_area");
            if (request.floor_area.HasValue)
                formData.Add(new StringContent(request.floor_area.Value.ToString()), "floor_area");
            if (request.original_value.HasValue)
                formData.Add(new StringContent(request.original_value.Value.ToString()), "original_value");
            if (request.remaining_value.HasValue)
                formData.Add(new StringContent(request.remaining_value.Value.ToString()), "remaining_value");
            formData.Add(new StringContent(request.asset_status ?? ""), "asset_status");
            formData.Add(new StringContent(request.installation_unit ?? ""), "installation_unit");
            formData.Add(new StringContent(request.management_unit ?? ""), "management_unit");
            formData.Add(new StringContent(request.custom_attributes ?? ""), "custom_attributes");

            // Xử lý file ảnh
            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for asset update with ID {AssetId}",
                    username, role, id);
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
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for asset update: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsync($"api/assets/{id}", formData));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} for update", username, role, id);
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update asset with ID {AssetId}: Invalid request - {Error}",
                    username, role, id, errorContent);
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update asset with ID {AssetId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update asset with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AssetsResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated asset with ID {AssetId} successfully", username, role, id);
            return result;
        }

        public async Task<bool> DeleteAssetAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting asset with ID {AssetId}", username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/assets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} for deletion", username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete asset with ID {AssetId}: {Error}", username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete asset with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete asset with ID {AssetId}: {StatusCode} - {Error}", username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete asset with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted asset with ID {AssetId} successfully", username, role, id);
            return true;
        }
    }
}