using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class AssetCagetoriesService : BaseService,IAssetCagetoriesService
    {
        public AssetCagetoriesService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<AssetCagetoriesResponse>> GetAllAssetCagetoriesAsync()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/assetCagetories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve asset categories: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AssetCagetoriesResponse>>(content);
        }

        public async Task<AssetCagetoriesResponse?> GetAssetCagetoriesByIdAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/assetCagetories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        public async Task<AssetCagetoriesResponse?> CreateAssetCagetoriesAsync(AssetCagetoriesRequest request)
        {
            var formData = new MultipartFormDataContent();

            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.category_name ?? ""), "category_name");
            formData.Add(new StringContent(request.geometry_type ?? ""), "geometry_type");
            formData.Add(new StringContent(request.attribute_schema ?? ""), "attribute_schema"); // Chuỗi JSON

            // Xử lý file ảnh
            if (request.sample_image != null && request.sample_image.Length > 0)
            {
                var fileContent = new StreamContent(request.sample_image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.sample_image.ContentType);
                formData.Add(fileContent, "sample_image", request.sample_image.FileName);
            }
            else
            {
                throw new ArgumentException("File ảnh mẫu là bắt buộc.");
            }
            if (request.icon != null && request.icon.Length > 0)
            {
                var fileContent = new StreamContent(request.icon.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.icon.ContentType);
                formData.Add(fileContent, "icon", request.icon.FileName);
            }
            else
            {
                throw new ArgumentException("File ảnh icon là bắt buộc.");
            }

            // Log dữ liệu gửi lên để debug
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

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/AssetCagetories", formData));
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
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        public async Task<AssetCagetoriesResponse?> UpdateAssetCagetoriesAsync(int id, AssetCagetoriesRequest request)
        {
            var formData = new MultipartFormDataContent();
            // Thêm các trường cơ bản
            formData.Add(new StringContent(request.category_name ?? ""), "category_name");
            formData.Add(new StringContent(request.geometry_type ?? ""), "geometry_type");
            formData.Add(new StringContent(request.attribute_schema ?? ""), "attribute_schema");   // Chuỗi JSON

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

            // Log dữ liệu gửi lên để debug
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

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsync($"api/assetCagetories/{id}", formData));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to update asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetCagetoriesResponse>(content);
        }

        public async Task<bool> DeleteAssetCagetoriesAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/assetCagetories/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete asset cagetory with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete asset cagetory with ID {id}: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
    }
}