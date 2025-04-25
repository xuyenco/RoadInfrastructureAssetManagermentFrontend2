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
    public class IncidentImageService : BaseService, IIncidentImageService
    {
        private readonly ILogger<IncidentImageService> _logger;

        public IncidentImageService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<IncidentImageService> logger)
            : base(httpClientFactory, httpContextAccessor, logger)
        {
            _logger = logger;
        }

        public async Task<List<IncidentImageResponse>> GetAllIncidentImagesAsync()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving all incident images", username, role);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/IncidentImages"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident images: Unauthorized access", username, role);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident images: {StatusCode} - {Error}",
                    username, role, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident images: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentImageResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incident images successfully",
                username, role, result?.Count ?? 0);
            return result;
        }

        public async Task<IncidentImageResponse?> GetIncidentImageByIdAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident image with ID {IncidentImageId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/IncidentImages/{id}"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident image with ID {IncidentImageId}: Unauthorized access",
                    username, role, id);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident image with ID {IncidentImageId}",
                    username, role, id);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident image with ID {IncidentImageId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident image with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentImageResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved incident image with ID {IncidentImageId} successfully",
                username, role, id);
            return result;
        }

        public async Task<List<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident images for incident ID {IncidentId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/IncidentImages/incidentid/{id}"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident images for incident ID {IncidentId}: Unauthorized access",
                    username, role, id);
                throw new UnauthorizedAccessException("Token expired or invalid. Please login again.");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to retrieve incident images for incident ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to retrieve incident images: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<IncidentImageResponse>>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {Count} incident images for incident ID {IncidentId} successfully",
                username, role, result?.Count ?? 0, id);
            return result;
        }

        public async Task<IncidentImageResponse?> CreateIncidentImageAsync(IncidentImageRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is creating incident image for incident ID {IncidentId}",
                username, role, request.incident_id);
            if (request.incident_id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid incident ID {IncidentId} for creating incident image",
                    username, role, request.incident_id);
                throw new ArgumentException("Incident ID must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.incident_id.ToString()), "incident_id");

            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included image for create: filename={FileName}, size={Size}",
                    username, role, request.image.FileName, request.image.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for creating incident image for incident ID {IncidentId}",
                    username, role, request.incident_id);
            }

            foreach (var item in formData)
            {
                var value = item switch
                {
                    StringContent stringContent => await stringContent.ReadAsStringAsync(),
                    StreamContent => "[File]",
                    _ => item.ToString()
                };
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for incident image creation: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/IncidentImages", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create incident image for incident ID {IncidentId}: Invalid request - {Error}",
                    username, role, request.incident_id, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to create incident image for incident ID {IncidentId}: {StatusCode} - {Error}",
                    username, role, request.incident_id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to create incident image: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentImageResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) created incident image with ID {IncidentImageId} successfully",
                username, role, result?.incident_image_id);
            return result;
        }

        public async Task<IncidentImageResponse?> UpdateIncidentImageAsync(int id, IncidentImageRequest request)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating incident image with ID {IncidentImageId} for incident ID {IncidentId}",
                username, role, id, request.incident_id);
            if (id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid incident image ID {IncidentImageId} for update",
                    username, role, id);
                throw new ArgumentException("IncidentImage ID must be a positive integer.");
            }
            if (request.incident_id <= 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid incident ID {IncidentId} for updating incident image",
                    username, role, request.incident_id);
                throw new ArgumentException("Incident ID must be a positive integer.");
            }

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(request.incident_id.ToString()), "incident_id");

            if (request.image != null && request.image.Length > 0)
            {
                var fileContent = new StreamContent(request.image.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.image.ContentType);
                formData.Add(fileContent, "image", request.image.FileName);
                _logger.LogDebug("User {Username} (Role: {Role}) included image for update: filename={FileName}, size={Size}",
                    username, role, request.image.FileName, request.image.Length);
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided no image for updating incident image with ID {IncidentImageId}",
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
                _logger.LogDebug("User {Username} (Role: {Role}) sending form data for incident image update: {Key} = {Value}",
                    username, role, item.Headers.ContentDisposition.Name, value);
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"api/IncidentImages/{id}")
            {
                Content = formData
            };
            var response = await ExecuteWithRefreshAsync(() => _httpClient.SendAsync(requestMessage));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update incident image with ID {IncidentImageId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to update incident image with ID {IncidentImageId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update incident image: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IncidentImageResponse>(content);
            _logger.LogInformation("User {Username} (Role: {Role}) updated incident image with ID {IncidentImageId} successfully",
                username, role, id);
            return result;
        }

        public async Task<bool> DeleteIncidentImageAsync(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username") ?? "anonymous";
            var role = _httpContextAccessor.HttpContext?.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is deleting incident image with ID {IncidentImageId}",
                username, role, id);
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/IncidentImages/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) failed to delete incident image with ID {IncidentImageId}: {StatusCode}",
                    username, role, id, response.StatusCode);
                return false;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("User {Username} (Role: {Role}) failed to delete incident image with ID {IncidentImageId}: {StatusCode} - {Error}",
                    username, role, id, response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to delete incident image with ID {id}: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("User {Username} (Role: {Role}) deleted incident image with ID {IncidentImageId} successfully",
                username, role, id);
            return true;
        }
    }
}