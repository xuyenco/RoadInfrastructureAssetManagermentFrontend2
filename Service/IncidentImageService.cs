using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class IncidentImageService : BaseService,IIncidentImageService
    {
        public IncidentImageService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }
        public async Task<List<IncidentImageResponse>> GetAllIncidentImagesAsync()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/IncidentImages"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expired or invalid. Please login againt.");
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentImageResponse>>(content);
        }

        // Get incidentImage by ID
        public async Task<IncidentImageResponse?> GetIncidentImageByIdAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/IncidentImages/{id}"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expired or invalid. Please login againt.");
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentImageResponse>(content);
        }

        public async Task<List<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/IncidentImages/incidentid/{id}"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Token expired or invalid. Please login againt.");
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentImageResponse>>(content);
        }

        // Create a new incidentImage
        public async Task<IncidentImageResponse?> CreateIncidentImageAsync(IncidentImageRequest request)
        {
            // Kiểm tra dữ liệu đầu vào
            if (request.incident_id <= 0)
            {
                throw new ArgumentException("Incident ID must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();

            // Thêm incident_id (bắt buộc)
            formData.Add(new StringContent(request.incident_id.ToString()), "incident_id");

            // Thêm file ảnh nếu tồn tại
            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                Console.WriteLine($"Image included in create: filename={request.image.FileName}, size={request.image.Length}");
            }
            else
            {
                Console.WriteLine("No image provided for create.");
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

            // Gửi yêu cầu POST
            var response = await ExecuteWithRefreshAsync (() => _httpClient.PostAsync("api/IncidentImages", formData));

            // Xử lý phản hồi
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create failed: {response.StatusCode} - {errorContent}");
                return null; // Trả về null nếu yêu cầu không hợp lệ
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Không thể tạo IncidentImage: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentImageResponse>(content);
        }

        public async Task<IncidentImageResponse?> UpdateIncidentImageAsync(int id, IncidentImageRequest request)
        {
            // Kiểm tra dữ liệu đầu vào
            if (id <= 0)
            {
                throw new ArgumentException("IncidentImage ID must be a positive integer.");
            }
            if (request.incident_id <= 0)
            {
                throw new ArgumentException("Incident ID must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();

            // Thêm incident_id (bắt buộc)
            formData.Add(new StringContent(request.incident_id.ToString()), "incident_id");

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
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/IncidentImages/{id}")
            {
                Content = formData
            };
            var response = await ExecuteWithRefreshAsync(() => _httpClient.SendAsync(requestMessage));

            // Xử lý phản hồi
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update failed: {response.StatusCode} - {errorContent}");
                return null; // Trả về null nếu không tìm thấy hoặc yêu cầu không hợp lệ
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Không thể cập nhật IncidentImage: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IncidentImageResponse>(content);
        }

        // Delete a incidentImage
        public async Task<bool> DeleteIncidentImageAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/IncidentImages/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                return false;
            }
            response.EnsureSuccessStatusCode();

            return true;
        }
    }
}
