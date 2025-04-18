using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Service
{
    public class MaintenanceDocumentService : BaseService, IMaintenanceDocumentService
    {
        public MaintenanceDocumentService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<MaintenanceDocumentResponse>> GetAllMaintenanceDocuments()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/MaintenanceDocument"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve maintenance document: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MaintenanceDocumentResponse>>(content);
        }

        public async Task<MaintenanceDocumentResponse?> GetMaintenanceDocumentById(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceDocument/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve  with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
        }

        public async Task<List<MaintenanceDocumentResponse>> GetMaintenanceDocumentByMaintenanceId(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceDocument/MaintenanceId/{id}"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve maintenance document by maintenance id: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MaintenanceDocumentResponse>>(content);
        }

        public async Task<MaintenanceDocumentResponse?> CreateMaintenanceDocument(MaintenanceDocumentRequest request)
        {
            // Kiểm tra dữ liệu đầu vào
            if (request.maintenance_id <= 0)
            {
                throw new ArgumentException("Maintenance Id must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();

            // Thêm maintenancedocument _id (bắt buộc)
            formData.Add(new StringContent(request.maintenance_id.ToString()), "maintenance_id");

            // Thêm file ảnh nếu tồn tại
            if (request.file != null && request.file.Length > 0)
            {
                var fileContent = new StreamContent(request.file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.file.ContentType);
                formData.Add(fileContent, "file", request.file.FileName);
                Console.WriteLine($"Image included in create: filename={request.file.FileName}, size={request.file.Length}");
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
            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/MaintenanceDocument", formData));

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
            return JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
        }

        public async Task<MaintenanceDocumentResponse?> UpdateMaintenanceDocument(int id, MaintenanceDocumentRequest request)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Maintenance document Id must be a positive integer.");
            }
            if (request.maintenance_id <= 0)
            {
                throw new ArgumentException("Maintenance Id must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();

            // Thêm maintenancedocumentid (bắt buộc)
            formData.Add(new StringContent(request.maintenance_id.ToString()), "maintenance_id");

            // Thêm file ảnh nếu tồn tại
            if (request.file != null && request.file.Length > 0)
            {
                var fileContent = new StreamContent(request.file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.file.ContentType);
                formData.Add(fileContent, "file", request.file.FileName);
                Console.WriteLine($"File included in update: filename={request.file.FileName}, size={request.file.Length}");
            }
            else
            {
                Console.WriteLine("No file provided for update.");
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/MaintenanceDocument/{id}")
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
                throw new HttpRequestException($"Không thể cập nhật Maintenance Document: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
        }

        public async Task<bool> DeleteMaintenanceDocument(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceDocument/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete maintenance document with ID {id}: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
        public async Task<bool> DeleteMaintenanceDocumentByMaintenanceId(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceDocument/MaintenanceId/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete maintenance document with ID {id}: {response.StatusCode} - {errorContent}");
            }

            return true;
        }
    }
}
