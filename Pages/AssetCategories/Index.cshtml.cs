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

        public IndexModel(IAssetCagetoriesService assetCagetoriesService)
        {
            _assetCagetoriesService = assetCagetoriesService;
        }

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        public async Task OnGetAsync()
        {
            var result = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();
            Categories = result?.ToList() ?? new List<AssetCagetoriesResponse>();
        }

        public async Task<IActionResult> OnGetGetDetailAsync(int id)
        {
            var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (category == null)
            {
                return Content("<p>Không tìm thấy danh mục.</p>");
            }

            // Debug dữ liệu
            var json = JsonSerializer.Serialize(category, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Get data by id:\n" + json);

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
                // Xử lý required
                var requiredFields = category.attribute_schema.TryGetValue("required", out var requiredValue)
                    ? requiredValue as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(requiredValue))
                    : new List<object>();
                html += $"<p><strong>Các trường bắt buộc:</strong> {(requiredFields.Count > 0 ? string.Join(", ", requiredFields) : "Không có")}</p>";

                // Xử lý properties
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

            // Thêm ảnh mẫu ở dưới cùng
            if (!string.IsNullOrEmpty(category.sample_image))
            {
                html += $"<p><strong>Ảnh mẫu:</strong> <img src='{category.sample_image}' alt='Sample Image' class='category-image'></p>";
            }

            html += "<a class=\"btn btn-secondary btn-sm\" href=\"/AssetCagetories/assetCagetoryUpdate/" + category.category_id + "\" data-toggle=\"tooltip\" title=\"Cập nhật Cagetory\">\r\n<i class=\"fas fa-edit\"></i> Cập nhật Cagetory\r\n</a>";
            return Content(html, "text/html");
        }
    }
}