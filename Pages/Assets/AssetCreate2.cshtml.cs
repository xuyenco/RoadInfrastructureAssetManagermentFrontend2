using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class AssetCreate2Model : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetCreate2Model> _logger;

        public AssetCreate2Model(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService, ILogger<AssetCreate2Model> logger)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        [BindProperty]
        public AssetsRequest NewAsset { get; set; } = new AssetsRequest();

        [BindProperty]
        public string GeometrySystem { get; set; }

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the asset creation page", username, role);
            Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {CategoryCount} categories for asset creation",username, role, Categories.Count);
        }

        public async Task<IActionResult> OnGetGetCategorySchemaAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving category schema for category ID {CategoryId}",username, role, id);

            var selectedCategories = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (selectedCategories == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no category with ID {CategoryId}", username, role, id);
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

            _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved category schema for category ID {CategoryId}",username, role, id);
            return new JsonResult(new
            {
                html,
                geometryType = selectedCategories.geometry_type ?? "Point"
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new asset", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}",username, role, JsonSerializer.Serialize(Request.Form));

            // Xử lý geometry (chuỗi JSON)
            var geometryJson = Request.Form["NewAsset.geometry"];
            if (!string.IsNullOrEmpty(geometryJson))
            {
                try
                {
                    NewAsset.geometry = geometryJson;
                    if (ModelState.ContainsKey("NewAsset.geometry"))
                    {
                        ModelState["NewAsset.geometry"].Errors.Clear();
                        ModelState["NewAsset.geometry"].ValidationState = ModelValidationState.Valid;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid GeoJSON: {Error}",username, role, ex.Message);
                    ModelState.AddModelError("NewAsset.geometry", $"Định dạng GeoJSON không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry", username, role);
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
                        var geoJsonGeometry = JsonSerializer.Deserialize<GeoJsonGeometry>(NewAsset.geometry);
                        var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(geoJsonGeometry, 48);
                        NewAsset.geometry = JsonSerializer.Serialize(vn2000Geometry);
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000: {Geometry}",username, role, NewAsset.geometry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to convert geometry to VN2000: {Error}",username, role, ex.Message);
                        ModelState.AddModelError("NewAsset.geometry", $"Lỗi chuyển đổi tọa độ: {ex.Message}");
                    }
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not select a geometry system", username, role);
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
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide custom attributes", username, role);
                ModelState.AddModelError("NewAsset.custom_attributes", "Thuộc tính tùy chỉnh là bắt buộc.");
            }

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors: {Errors}",username, role, JsonSerializer.Serialize(errors));
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) sending asset creation data: {AssetData}",username, role, JsonSerializer.Serialize(NewAsset));
                var createdAsset = await _assetsService.CreateAssetAsync(NewAsset);
                if (createdAsset == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create asset: No result returned",username, role);
                    ModelState.AddModelError("", "Không thể tạo tài sản.");
                    Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                    return Page();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) successfully created asset with ID {AssetId}",username, role, createdAsset.asset_id);
                return RedirectToPage("/Assets/Index");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) received BadRequest from API: {Error}",username, role, ex.Message);
                ModelState.AddModelError("", $"Lỗi từ API: {ex.Message}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error creating asset: {Error}",username, role, ex.Message);
                ModelState.AddModelError("", $"Lỗi khi tạo tài sản: {ex.Message}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }
        }
    }
}