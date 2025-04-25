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
    public class MaintenanceDocumentService : BaseService, IMaintenanceDocumentService
    {
        private readonly ILogger<MaintenanceDocumentService> _logger;

        public MaintenanceDocumentService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<MaintenanceDocumentService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<MaintenanceDocumentResponse>> GetAllMaintenanceDocuments()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all maintenance documents", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/MaintenanceDocument"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance documents: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance document: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<MaintenanceDocumentResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} maintenance documents successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<MaintenanceDocumentResponse?> GetMaintenanceDocumentById(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance document with ID {MaintenanceDocumentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceDocument/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance document with ID {MaintenanceDocumentId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance document with ID {MaintenanceDocumentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance document with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved maintenance document with ID {MaintenanceDocumentId} successfully",
                username, role, id);
            return result;
        }

        public async Task<List<MaintenanceDocumentResponse>> GetMaintenanceDocumentByMaintenanceId(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance documents for maintenance ID {MaintenanceId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/MaintenanceDocument/MaintenanceId/{id}"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve maintenance documents for maintenance ID {MaintenanceId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve maintenance document by maintenance id: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<MaintenanceDocumentResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} maintenance documents for maintenance ID {MaintenanceId} successfully",
                username, role, result?.Count ?? 0, id);
            return result;
        }

        public async Task<MaintenanceDocumentResponse?> CreateMaintenanceDocument(MaintenanceDocumentRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating maintenance document for maintenance ID {MaintenanceId}",
                username, role, request.maintenance_id);
            if (request.maintenance_id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid maintenance ID {MaintenanceId} for creating maintenance document",
                    username, role, request.maintenance_id);
                throw new ArgumentException("Maintenance Id must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.maintenance_id.ToString()), "maintenance_id");

            if (request.file != null && request.file.Length > 0)
            {
                var fileContent = new StreamContent(request.file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.file.ContentType);
                formData.Add(fileContent, "file", request.file.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included file for create: filename={FileName}, size={Size}",
                    username, role, request.file.FileName, request.file.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no file for creating maintenance document for maintenance ID {MaintenanceId}",
                    username, role, request.maintenance_id);
            }

            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for maintenance document creation: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/MaintenanceDocument", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create maintenance document for maintenance ID {MaintenanceId}: Invalid request - {Error}",
                    username, role, request.maintenance_id, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create maintenance document for maintenance ID {MaintenanceId}: {StatusCode} - {Error}",
                    username, role, request.maintenance_id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create maintenance document: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created maintenance document with ID {MaintenanceDocumentId} successfully",
                username, role, result?.document_id);
            return result;
        }

        public async Task<MaintenanceDocumentResponse?> UpdateMaintenanceDocument(int id, MaintenanceDocumentRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating maintenance document with ID {MaintenanceDocumentId} for maintenance ID {MaintenanceId}",
                username, role, id, request.maintenance_id);
            if (id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid maintenance document ID {MaintenanceDocumentId} for update",
                    username, role, id);
                throw new ArgumentException("Maintenance document Id must be a positive integer.");
            }
            if (request.maintenance_id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid maintenance ID {MaintenanceId} for updating maintenance document",
                    username, role, request.maintenance_id);
                throw new ArgumentException("Maintenance Id must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.maintenance_id.ToString()), "maintenance_id");

            if (request.file != null && request.file.Length > 0)
            {
                var fileContent = new StreamContent(request.file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.file.ContentType);
                formData.Add(fileContent, "file", request.file.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included file for update: filename={FileName}, size={Size}",
                    username, role, request.file.FileName, request.file.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no file for updating maintenance document with ID {MaintenanceDocumentId}",
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
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for maintenance document update: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/MaintenanceDocument/{id}")
            {
                Content = formData
            };
            var response = await ExecuteWithRefreshAsync(() => _httpClient.SendAsync(requestMessage));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update maintenance document with ID {MaintenanceDocumentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update maintenance document with ID {MaintenanceDocumentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update maintenance document: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MaintenanceDocumentResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated maintenance document with ID {MaintenanceDocumentId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteMaintenanceDocument(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting maintenance document with ID {MaintenanceDocumentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceDocument/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance document with ID {MaintenanceDocumentId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance document with ID {MaintenanceDocumentId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance document with ID {MaintenanceDocumentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete maintenance document with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted maintenance document with ID {MaintenanceDocumentId} successfully",
                username, role, id);
            return true;
        }

        public async Task<bool> DeleteMaintenanceDocumentByMaintenanceId(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting maintenance documents for maintenance ID {MaintenanceId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/MaintenanceDocument/MaintenanceId/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance documents for maintenance ID {MaintenanceId} for deletion",
                    username, role, id);
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance documents for maintenance ID {MaintenanceId}: {Error}",
                    username, role, id, errorContent);
                throw new InvalidOperationException($"Failed to delete maintenance document with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete maintenance documents for maintenance ID {MaintenanceId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete maintenance document with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted maintenance documents for maintenance ID {MaintenanceId} successfully",
                username, role, id);
            return true;
        }
    }
}