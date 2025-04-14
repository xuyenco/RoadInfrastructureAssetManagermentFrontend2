using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;

using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.AssetCagetories
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
                <h3>{category.cagetory_name}</h3>
                <p><strong>ID:</strong> {category.cagetory_id}</p>
                <p><strong>Loại hình học:</strong> {category.geometry_type}</p>
                <p><strong>Ngày tạo:</strong> {(category.created_at?.ToString("dd/MM/yyyy HH:mm") ?? "N/A")}</p>
                <h4>Thuộc tính (Attributes Schema)</h4>";

            // Xử lý required
            var requiredFields = category.attributes_schema.TryGetValue("required", out var requiredValue)
                ? requiredValue as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(requiredValue))
                : new List<object>();
            html += $"<p><strong>Các trường bắt buộc:</strong> {(requiredFields.Count > 0 ? string.Join(", ", requiredFields) : "Không có")}</p>";

            // Xử lý properties
            html += "<h5>Properties:</h5><ul>";
            if (category.attributes_schema.TryGetValue("properties", out var propertiesValue) && propertiesValue != null)
            {
                // Deserialize propertiesValue thành Dictionary<string, object>
                var propertiesJson = JsonSerializer.Serialize(propertiesValue); // Chuyển thành chuỗi JSON
                var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);

                if (properties != null && properties.Count > 0)
                {
                    foreach (var prop in properties)
                    {
                        var detailsJson = JsonSerializer.Serialize(prop.Value); // Chuyển value thành chuỗi JSON
                        var details = JsonSerializer.Deserialize<Dictionary<string, object>>(detailsJson);

                        html += $"<li><strong>{prop.Key}:</strong> ";
                        if (details != null)
                        {
                            html += $"Type: {(details.TryGetValue("type", out var typeValue) ? typeValue : "N/A")}, ";
                            html += $"Description: {(details.TryGetValue("description", out var descriptionValue) ? descriptionValue : "N/A")}";
                            if (details.TryGetValue("enum", out var enumObj) && enumObj != null)
                            {
                                // Kiểm tra xem enumObj đã là List<object> chưa, nếu không thì deserialize
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
                html += "<li>Không có thuộc tính nào hoặc dữ liệu không đúng định dạng.</li>";
            }

            html += $@"
                </ul>
                <h4>Giai đoạn (Lifecycle Stages)</h4>
                <ul>";

            foreach (var stage in category.lifecycle_stages ?? new List<string>())
            {
                html += $"<li>{stage}</li>";
            }

            html += "</ul>";
html += "<a class=\"btn btn-secondary btn-sm\" href=\"/AssetCagetories/assetCagetoryUpdate/" + category.cagetory_id + "\" data-toggle=\"tooltip\" title=\"Cập nhật Cagetory\">\r\n<i class=\"fas fa-edit\"></i> Cập nhật Cagetory\r\n</a>";
            return Content(html, "text/html");
        }
    }
}
