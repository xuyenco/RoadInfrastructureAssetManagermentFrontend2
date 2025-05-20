using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.AssetCategories
{
    public class IndexModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<IndexModel> _logger; 

        public IndexModel(IAssetCagetoriesService assetCagetoriesService, ILogger<IndexModel> logger)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing Asset Categories Index page", username, role); // Log truy cập trang
            try
            {
                var result = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();
                Categories = result?.ToList() ?? new List<AssetCagetoriesResponse>();
                _logger.LogInformation("User {Username} (Role: {Role}) loaded {Count} asset categories", username, role, Categories.Count); // Log thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} (Role: {Role}) failed to load asset categories", username, role); // Log lỗi
                Categories = new List<AssetCagetoriesResponse>();
            }
        }

        public async Task<IActionResult> OnGetGetDetailAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving details for asset category with ID {CategoryId}", username, role, id);
            try
            {
                var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId}", username, role, id);
                    return Content("<p class='text-red-500'>Không tìm thấy danh mục.</p>", "text/html");
                }

                var json = JsonSerializer.Serialize(category, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("User {Username} (Role: {Role}) retrieved asset category details: {Data}", username, role, json);

                // Tạo HTML chi tiết với Tailwind CSS, bỏ max-w-lg để nội dung đầy đủ
                var html = $@"
                <h1 class='text-3xl font-bold text-gray-800 mb-6'>Chi tiết danh mục tài sản</h1>
                <div class='bg-white rounded-lg shadow-lg p-6'>
                    <h3 class='text-2xl font-semibold text-gray-800 mb-4'>{category.category_name}</h3>
                    <div class='space-y-3'>
                        <p><strong class='text-gray-700'>ID:</strong> {category.category_id}</p>
                        <p><strong class='text-gray-700'>Loại hình học:</strong> {category.geometry_type}</p>
                        <p><strong class='text-gray-700'>Ngày tạo:</strong> {(category.created_at?.ToString("dd/MM/yyyy HH:mm") ?? "N/A")}</p>
                    </div>

                    <h4 class='text-xl font-semibold text-gray-700 mt-6 mb-3 border-b-2 border-blue-500 pb-2'>Thuộc tính (Attribute Schema)</h4>";

                if (category.attribute_schema != null && category.attribute_schema.Count > 0)
                {
                    var requiredFields = category.attribute_schema.TryGetValue("required", out var requiredValue)
                        ? requiredValue as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(requiredValue))
                        : new List<object>();
                    html += $"<p class='mb-3'><strong class='text-gray-700'>Các trường bắt buộc:</strong> {(requiredFields.Count > 0 ? string.Join(", ", requiredFields) : "Không có")}</p>";

                    html += "<h5 class='text-lg font-semibold text-gray-600 mb-2'>Properties:</h5><ul class='list-disc pl-5 space-y-2'>";
                    if (category.attribute_schema.TryGetValue("properties", out var propertiesValue) && propertiesValue != null)
                    {
                        var propertiesJson = JsonSerializer.Serialize(propertiesValue);
                        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);

                        if (properties != null && properties.Count > 0)
                        {
                            foreach (var prop in properties)
                            {
                                var detailsJson = JsonSerializer.Serialize(prop.Value);
                                var details = JsonSerializer.Deserialize<Dictionary<string, object>>(detailsJson);

                                html += $"<li class='text-gray-700'><strong>{prop.Key}:</strong> ";
                                if (details != null)
                                {
                                    html += $"Type: {(details.TryGetValue("type", out var typeValue) ? typeValue : "N/A")}, ";
                                    html += $"Description: {(details.TryGetValue("description", out var descriptionValue) ? descriptionValue : "N/A")}";
                                    if (details.TryGetValue("enum", out var enumObj) && enumObj != null)
                                    {
                                        var enumValues = enumObj as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(enumObj));
                                        html += $", Enum: {string.Join(", ", enumValues)}";
                                    }
                                }
                                else
                                {
                                    html += "N/A";
                                }
                                html += "</li>";
                            }
                        }
                        else
                        {
                            html += "<li class='text-gray-500'>Không có thuộc tính nào.</li>";
                        }
                    }
                    else
                    {
                        html += "<li class='text-gray-500'>Không có thuộc tính nào.</li>";
                    }
                    html += "</ul>";
                }
                else
                {
                    html += "<p class='text-gray-500'>Không có thông tin schema.</p>";
                }

                if (!string.IsNullOrEmpty(category.sample_image))
                {
                    html += $@"
                    <p class='mt-4'>
                        <strong class='text-gray-700'>Ảnh mẫu:</strong>
                        <img src='{category.sample_image}' alt='Sample Image' class='mt-2 w-full max-w-sm rounded-lg shadow-md hover:shadow-lg transition-shadow duration-200'>
                    </p>";
                }

                html += $@"
                    <div class='mt-6 text-center'>
                        <a href='/AssetCategories/AssetCategoryUpdate/{category.category_id}' class='bg-gray-600 text-white px-4 py-2 rounded-lg hover:bg-gray-700 transition duration-200 inline-flex items-center' data-toggle='tooltip' title='Cập nhật Category'>
                            <i class='fas fa-edit mr-2'></i> Cập nhật Category
                        </a>
                    </div>
                </div>";

                _logger.LogInformation("User {Username} (Role: {Role}) retrieved details for asset category with ID {CategoryId} successfully", username, role, id);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} (Role: {Role}) failed to retrieve details for asset category with ID {CategoryId}", username, role, id);
                return Content("<p class='text-red-500'>Đã xảy ra lỗi khi tải chi tiết.</p>", "text/html");
            }
        }
    }
}