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

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving details for asset category with ID {CategoryId}", username, role, id); // Log gọi AJAX
            try
            {
                var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId}", username, role, id); // Log không tìm thấy
                    return Content("<p>Không tìm thấy danh mục.</p>");
                }

                // Log dữ liệu trả về (thay Console.WriteLine)
                var json = JsonSerializer.Serialize(category, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("User {Username} (Role: {Role}) retrieved asset category details: {Data}", username, role, json);

                // Tạo HTML chi tiết
                var html = $@"
                    <h3>{category.category_name}</h3>
                    <p><strong>ID:</strong> {category.category_id}</p>
                    <p><strong>Loại hình học:</strong> {category.geometry_type}</p>
                    <p><strong>Ngày tạo:</strong> {(category.created_at?.ToString("dd/MM/yyyy HH:mm") ?? "N/A")}</p>
                    <h4>Thuộc tính (Attribute Schema)</h4>";

                // Xử lý attribute_schema
                if (category.attribute_schema != null && category.attribute_schema.Count > 0)
                {
                    var requiredFields = category.attribute_schema.TryGetValue("required", out var requiredValue)
                        ? requiredValue as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(requiredValue))
                        : new List<object>();
                    html += $"<p><strong>Các trường bắt buộc:</strong> {(requiredFields.Count > 0 ? string.Join(", ", requiredFields) : "Không có")}</p>";

                    html += "<h5>Properties:</h5><ul>";
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

                                html += $"<li><strong>{prop.Key}:</strong> ";
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
                            html += "<li>Không có thuộc tính nào.</li>";
                        }
                    }
                    else
                    {
                        html += "<li>Không có thuộc tính nào.</li>";
                    }
                    html += "</ul>";
                }
                else
                {
                    html += "<p>Không có thông tin schema.</p>";
                }

                if (!string.IsNullOrEmpty(category.sample_image))
                {
                    html += $"<p><strong>Ảnh mẫu:</strong> <img src='{category.sample_image}' alt='Sample Image' class='category-image'></p>";
                }

                html += "<a class=\"btn btn-secondary btn-sm\" href=\"/AssetCagetories/assetCagetoryUpdate/" + category.category_id + "\" data-toggle=\"tooltip\" title=\"Cập nhật Cagetory\">\r\n<i class=\"fas fa-edit\"></i> Cập nhật Cagetory\r\n</a>";
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved details for asset category with ID {CategoryId} successfully", username, role, id); // Log thành công
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} (Role: {Role}) failed to retrieve details for asset category with ID {CategoryId}", username, role, id); // Log lỗi
                return Content("<p class=\"text-danger\">Đã xảy ra lỗi khi tải chi tiết.</p>", "text/html");
            }
        }
    }
}