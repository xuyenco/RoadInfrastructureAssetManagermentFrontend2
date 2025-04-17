using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class AssetsService : BaseService, IAssetsService
    {
        public AssetsService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<AssetsResponse>> GetAllAssetsAsync()
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync("api/assets"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve assets: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AssetsResponse>>(content);
        }

        public async Task<AssetsResponse?> GetAssetByIdAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.GetAsync($"api/assets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve asset with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        public async Task<AssetsResponse?> CreateAssetAsync(AssetsRequest request)
        {
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

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PostAsync("api/assets", formData));
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Invalid request: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create asset: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        public async Task<AssetsResponse?> UpdateAssetAsync(int id, AssetsRequest request)
        {
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

            var response = await ExecuteWithRefreshAsync(() => _httpClient.PatchAsync($"api/assets/{id}", formData));
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
                throw new HttpRequestException($"Failed to update asset with ID {id}: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AssetsResponse>(content);
        }

        public async Task<bool> DeleteAssetAsync(int id)
        {
            var response = await ExecuteWithRefreshAsync(() => _httpClient.DeleteAsync($"api/assets/{id}"));
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to delete asset with ID {id}: {errorContent}");
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete asset with ID {id}: {response.StatusCode} - {errorContent}");
            }
            return true;
        }
    }
}