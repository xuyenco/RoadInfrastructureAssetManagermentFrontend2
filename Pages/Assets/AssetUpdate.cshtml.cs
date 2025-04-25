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
    public class AssetUpdateModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetUpdateModel> _logger;

        public AssetUpdateModel(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService, ILogger<AssetUpdateModel> logger)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        public AssetsResponse AssetResponse { get; set; }
        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        [BindProperty]
        public AssetsRequest AssetRequest { get; set; }

        [BindProperty(SupportsGet = true)]
        public int AssetId { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset with ID {AssetId} for update", username, role, id);
            AssetId = id;
            AssetResponse = await _assetsService.GetAssetByIdAsync(id);
            if (AssetResponse == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId}", username, role, id);
                return NotFound();
            }

            AssetRequest = new AssetsRequest
            {
                category_id = AssetResponse.category_id,
                asset_name = AssetResponse.asset_name,
                asset_code = AssetResponse.asset_code,
                address = AssetResponse.address,
                construction_year = AssetResponse.construction_year,
                operation_year = AssetResponse.operation_year,
                land_area = AssetResponse.land_area,
                floor_area = AssetResponse.floor_area,
                original_value = AssetResponse.original_value,
                remaining_value = AssetResponse.remaining_value,
                asset_status = AssetResponse.asset_status,
                installation_unit = AssetResponse.installation_unit,
                management_unit = AssetResponse.management_unit,
                geometry = JsonSerializer.Serialize(AssetResponse.geometry ?? new GeoJsonGeometry()),
                custom_attributes = JsonSerializer.Serialize(AssetResponse.custom_attributes ?? new Dictionary<string, object>())
            };

            Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
            _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved asset with ID {AssetId} and {CategoryCount} categories",username, role, id, Categories.Count);
            _logger.LogDebug("User {Username} (Role: {Role}) retrieved asset data: {AssetRequest}",username, role, JsonSerializer.Serialize(AssetRequest));
            return Page();
        }

        public async Task<IActionResult> OnGetGetCategorySchemaAsync(int id, int assetId)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving category schema for category ID {CategoryId} and asset ID {AssetId}",
                username, role, id, assetId);

            AssetResponse = await _assetsService.GetAssetByIdAsync(assetId);
            if (AssetResponse == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId}", username, role, assetId);
                return new JsonResult(new { html = "<p>Không tìm thấy tài sản.</p>" });
            }

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
                        <div class='form-group mb-3'>
                            <label for='{prop.Key}'>{prop.Key} {(isRequired ? "(bắt buộc)" : "")}</label>";
                    if (details.TryGetValue("enum", out var enumObj) && enumObj != null)
                    {
                        var enumValues = enumObj as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(enumObj));
                        html += $"<select id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")}>";
                        html += "<option value=''>Chọn</option>";
                        foreach (var val in enumValues)
                        {
                            html += $"<option value='{val}' {(AssetResponse.custom_attributes != null && AssetResponse.custom_attributes.TryGetValue(prop.Key, out var value) && value?.ToString() == val.ToString() ? "selected" : "")}>{val}</option>";
                        }
                        html += "</select>";
                    }
                    else if (inputType == "integer" || inputType == "number")
                    {
                        html += $"<input type='number' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' value='{(AssetResponse.custom_attributes != null && AssetResponse.custom_attributes.TryGetValue(prop.Key, out var value) ? value : "")}' {(isRequired ? "required" : "")} />";
                    }
                    else if (inputType == "date")
                    {
                        html += $"<input type='date' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' value='{(AssetResponse.custom_attributes != null && AssetResponse.custom_attributes.TryGetValue(prop.Key, out var value) ? value : "")}' {(isRequired ? "required" : "")} />";
                    }
                    else
                    {
                        html += $"<input type='text' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' value='{(AssetResponse.custom_attributes != null && AssetResponse.custom_attributes.TryGetValue(prop.Key, out var value) ? value : "")}' {(isRequired ? "required" : "")} />";
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

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting update for asset with ID {AssetId}", username, role, AssetId);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}",username, role, JsonSerializer.Serialize(Request.Form));

            // Xử lý geometry (chuỗi JSON)
            var geometryJson = Request.Form["AssetRequest.geometry"];
            if (!string.IsNullOrEmpty(geometryJson))
            {
                try
                {
                    AssetRequest.geometry = geometryJson;
                    if (ModelState.ContainsKey("AssetRequest.geometry"))
                    {
                        ModelState["AssetRequest.geometry"].Errors.Clear();
                        ModelState["AssetRequest.geometry"].ValidationState = ModelValidationState.Valid;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid GeoJSON for asset with ID {AssetId}: {Error}",username, role, AssetId, ex.Message);
                    ModelState.AddModelError("AssetRequest.geometry", $"Định dạng GeoJSON không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry for asset with ID {AssetId}",username, role, AssetId);
                ModelState.AddModelError("AssetRequest.geometry", "Hình học là bắt buộc.");
            }

            // Xử lý GeometrySystem
            if (!string.IsNullOrEmpty(GeometrySystem) && !string.IsNullOrEmpty(AssetRequest.geometry))
            {
                if (GeometrySystem == "wgs84")
                {
                    try
                    {
                        var geoJsonGeometry = JsonSerializer.Deserialize<GeoJsonGeometry>(AssetRequest.geometry);
                        var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(geoJsonGeometry, 48);
                        AssetRequest.geometry = JsonSerializer.Serialize(vn2000Geometry);
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for asset with ID {AssetId}: {Geometry}",
                            username, role, AssetId, AssetRequest.geometry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to convert geometry to VN2000 for asset with ID {AssetId}: {Error}",username, role, AssetId, ex.Message);
                        ModelState.AddModelError("AssetRequest.geometry", $"Lỗi chuyển đổi tọa độ: {ex.Message}");
                    }
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not select a geometry system for asset with ID {AssetId}",
                    username, role, AssetId);
                ModelState.AddModelError("GeometrySystem", "Phải chọn loại hệ thống địa lý");
            }

            // Xử lý custom_attributes
            var customAttributesJson = Request.Form["AssetRequest.custom_attributes"];
            if (!string.IsNullOrEmpty(customAttributesJson))
            {
                AssetRequest.custom_attributes = customAttributesJson;
                if (ModelState.ContainsKey("AssetRequest.custom_attributes"))
                {
                    ModelState["AssetRequest.custom_attributes"].Errors.Clear();
                    ModelState["AssetRequest.custom_attributes"].ValidationState = ModelValidationState.Valid;
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide custom attributes for asset with ID {AssetId}",
                    username, role, AssetId);
                ModelState.AddModelError("AssetRequest.custom_attributes", "Thuộc tính tùy chỉnh là bắt buộc.");
            }

            // Xử lý hình ảnh
            var imageFile = Request.Form.Files["AssetRequest.image"];
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    using var stream = new MemoryStream();
                    await imageFile.CopyToAsync(stream);
                    AssetRequest.image = new FormFile(stream, 0, stream.Length, imageFile.Name, imageFile.FileName);
                    _logger.LogDebug("User {Username} (Role: {Role}) uploaded new image for asset with ID {AssetId}: filename={FileName}, size={Size}",
                        username, role, AssetId, imageFile.FileName, imageFile.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to upload image for asset with ID {AssetId}: {Error}",
                        username, role, AssetId, ex.Message);
                    ModelState.AddModelError("AssetRequest.image", $"Lỗi tải lên hình ảnh: {ex.Message}");
                }
            }
            else
            {
                // Giữ ảnh cũ nếu không chọn ảnh mới
                if (!string.IsNullOrEmpty(AssetResponse?.image_url))
                {
                    try
                    {
                        var imageBytes = Convert.FromBase64String(AssetResponse.image_url.Split(',')[1]);
                        var stream = new MemoryStream(imageBytes);
                        AssetRequest.image = new FormFile(stream, 0, stream.Length, "image", "existing_image.jpg");
                        _logger.LogDebug("User {Username} (Role: {Role}) reused existing image for asset with ID {AssetId}",
                            username, role, AssetId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to reuse existing image for asset with ID {AssetId}: {Error}",
                            username, role, AssetId, ex.Message);
                        ModelState.AddModelError("AssetRequest.image", $"Lỗi xử lý ảnh hiện tại: {ex.Message}");
                    }
                }
            }

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors while updating asset with ID {AssetId}: {Errors}",username, role, AssetId, JsonSerializer.Serialize(errors));
                AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                if (AssetResponse == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} after validation errors",username, role, AssetId);
                    return NotFound();
                }
                return Page();
            }

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) sending update data for asset with ID {AssetId}: {AssetRequest}",username, role, AssetId, JsonSerializer.Serialize(AssetRequest));
                var updatedAsset = await _assetsService.UpdateAssetAsync(AssetId, AssetRequest);
                if (updatedAsset == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to update asset with ID {AssetId}: No result returned",username, role, AssetId);
                    ModelState.AddModelError("", "Cập nhật thất bại.");
                    AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                    Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                    if (AssetResponse == null)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} after failed update",
                            username, role, AssetId);
                        return NotFound();
                    }
                    return Page();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) successfully updated asset with ID {AssetId}",username, role, AssetId);
                return RedirectToPage("/Assets/Index");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) received BadRequest from API while updating asset with ID {AssetId}: {Error}",username, role, AssetId, ex.Message);
                ModelState.AddModelError("", $"Lỗi từ API: {ex.Message}");
                AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                if (AssetResponse == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} after BadRequest",username, role, AssetId);
                    return NotFound();
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered an error while updating asset with ID {AssetId}: {Error}",username, role, AssetId, ex.Message);
                ModelState.AddModelError("", $"Lỗi khi cập nhật tài sản: {ex.Message}");
                AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                if (AssetResponse == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId} after error",
                        username, role, AssetId);
                    return NotFound();
                }
                return Page();
            }
        }
    }
}