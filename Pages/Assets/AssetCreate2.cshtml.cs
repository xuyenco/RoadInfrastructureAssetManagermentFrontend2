using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Assets
{
    public class AssetCreate2Model : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public AssetCreate2Model(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public AssetsRequest NewAsset { get; set; } = new AssetsRequest();

        [BindProperty]
        public string GeometrySystem { get; set; }

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        public async Task OnGetAsync()
        {
            Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
        }

        public async Task<IActionResult> OnGetGetCategorySchemaAsync(int id)
        {
            var selectedCategories = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (selectedCategories == null)
            {
                return new JsonResult(new { html = "<p>Không tìm thấy danh mục.</p>" });
            }

            var html = "";
            var properties = selectedCategories.attribute_schema != null && selectedCategories.attribute_schema.TryGetValue("properties", out var props) && props != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(props))
                : new Dictionary<string, object>();
            var requiredFields = selectedCategories.attribute_schema != null && selectedCategories.attribute_schema.TryGetValue("required", out var req) && req != null
                ? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(req))
                : new List<object>();

            if (properties.Count == 0)
            {
                html = "<p>Không có thuộc tính nào cho danh mục này.</p>";
            }
            else
            {
                foreach (var prop in properties)
                {
                    var details = prop.Value != null
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(prop.Value))
                        : new Dictionary<string, object>();
                    var isRequired = requiredFields.Contains(prop.Key);
                    var inputType = details.TryGetValue("type", out var type) ? type.ToString() : "string";

                    html += $@"
                <div class='form-group'>
                    <label for='{prop.Key}'>{prop.Key} {(isRequired ? "(bắt buộc)" : "")}</label>";
                    if (details.TryGetValue("enum", out var enumObj) && enumObj != null)
                    {
                        var enumValues = enumObj as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(enumObj));
                        html += $"<select id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")}>";
                        html += "<option value=''>Chọn</option>";
                        foreach (var val in enumValues)
                        {
                            html += $"<option value='{val}'>{val}</option>";
                        }
                        html += "</select>";
                    }
                    else if (inputType == "integer" || inputType == "number")
                    {
                        html += $"<input type='number' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")} />";
                    }
                    else if (inputType == "date")
                    {
                        html += $"<input type='date' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")} />";
                    }
                    else
                    {
                        html += $"<input type='text' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")} />";
                    }
                    html += "</div>";
                }
            }

            return new JsonResult(new
            {
                html,
                geometryType = selectedCategories.geometry_type ?? "Point"
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("Post method start - Request received");
            Console.WriteLine($"Form data: {JsonSerializer.Serialize(Request.Form)}");

            // Xử lý geometry (chuỗi JSON)
            var geometryJson = Request.Form["NewAsset.geometry"];
            if (!string.IsNullOrEmpty(geometryJson))
            {
                try
                {
                    // Gán trực tiếp chuỗi JSON vào NewAsset.geometry
                    NewAsset.geometry = geometryJson;
                    if (ModelState.ContainsKey("NewAsset.geometry"))
                    {
                        ModelState["NewAsset.geometry"].Errors.Clear();
                        ModelState["NewAsset.geometry"].ValidationState = ModelValidationState.Valid;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewAsset.geometry", $"Định dạng GeoJSON không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.geometry", "Hình học là bắt buộc.");
            }

            // Xử lý GeometrySystem (chuyển đổi tọa độ nếu cần)
            var geometrySystem = Request.Form["GeometrySystem"];
            if (!string.IsNullOrEmpty(geometrySystem) && !string.IsNullOrEmpty(NewAsset.geometry))
            {
                if (geometrySystem == "wgs84")
                {
                    try
                    {
                        // Parse tạm geometry để chuyển đổi tọa độ
                        var geoJsonGeometry = JsonSerializer.Deserialize<GeoJsonGeometry>(NewAsset.geometry);
                        var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(geoJsonGeometry, 48);
                        // Gán lại chuỗi JSON sau khi chuyển đổi
                        NewAsset.geometry = JsonSerializer.Serialize(vn2000Geometry);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("NewAsset.geometry", $"Lỗi chuyển đổi tọa độ: {ex.Message}");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("GeometrySystem", "Phải chọn loại hệ thống địa lý");
            }

            // Xử lý custom_attributes
            var customAttributesJson = Request.Form["NewAsset.custom_attributes"];
            if (!string.IsNullOrEmpty(customAttributesJson))
            {
                NewAsset.custom_attributes = customAttributesJson;
                if (ModelState.ContainsKey("NewAsset.custom_attributes"))
                {
                    ModelState["NewAsset.custom_attributes"].Errors.Clear();
                    ModelState["NewAsset.custom_attributes"].ValidationState = ModelValidationState.Valid;
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.custom_attributes", "Thuộc tính tùy chỉnh là bắt buộc.");
            }

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }

            try
            {
                var createdAsset = await _assetsService.CreateAssetAsync(NewAsset);
                if (createdAsset == null)
                {
                    ModelState.AddModelError("", "Không thể tạo tài sản.");
                    Console.WriteLine("Asset creation returned null");
                    Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                    return Page();
                }
                Console.WriteLine("Asset created successfully");
                return RedirectToPage("/Assets/Index");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError("", $"Lỗi từ API: {ex.Message}");
                Console.WriteLine($"BadRequest from API: {ex.Message}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi tạo tài sản: {ex.Message}");
                Console.WriteLine($"Error creating asset: {ex.Message}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }
        }
    }
}