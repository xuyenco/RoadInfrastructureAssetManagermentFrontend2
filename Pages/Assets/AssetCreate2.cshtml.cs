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
        public string GeometrySystem { get; set; }

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();
        public AssetCagetoriesResponse SelectedCategories { get; set; }


        public async Task OnGetAsync()
        {
            Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
        }

        public async Task<IActionResult> OnGetGetCategorySchemaAsync(int id)
        {
            var SelectedCategories = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (SelectedCategories == null)
            {
                return new JsonResult(new { html = "<p>Không tìm thấy danh mục.</p>" });
            }

            var html = "";
            var properties = SelectedCategories.attributes_schema != null && SelectedCategories.attributes_schema.TryGetValue("properties", out var props) && props != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(props))
                : new Dictionary<string, object>();
            var requiredFields = SelectedCategories.attributes_schema != null && SelectedCategories.attributes_schema.TryGetValue("required", out var req) && req != null
                ? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(req))
                : new List<object>();

            // Lấy lifecycle_stages trực tiếp từ SelectedCategories
            var lifecycleStages = SelectedCategories.lifecycle_stages ?? new List<string>(); // Nếu null, trả về mảng rỗng

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
                    Console.WriteLine($"Du lieu nhap dao duoi dang:{inputType}");

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
                geometryType = SelectedCategories.geometry_type ?? "Point",
                lifecycleStages = lifecycleStages // Trả về trực tiếp từ SelectedCategories
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("Post method start - Request received");
            Console.WriteLine($"Form data: {JsonSerializer.Serialize(Request.Form)}");

            if (NewAsset.geometry == null) NewAsset.geometry = new GeoJsonGeometry();

            

            // Xử lý geometry.type
            var geometryType = Request.Form["NewAsset.geometry.type"];
            if (!string.IsNullOrEmpty(geometryType))
            {
                NewAsset.geometry.type = geometryType;
                // Xóa lỗi ModelState cho geometry.type nếu gán thành công
                if (ModelState.ContainsKey("NewAsset.geometry.type"))
                {
                    ModelState["NewAsset.geometry.type"].Errors.Clear();
                    ModelState["NewAsset.geometry.type"].ValidationState = ModelValidationState.Valid;
                    Console.WriteLine("Cleared ModelState errors for NewAsset.geometry.type");
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.geometry.type", "Loại hình học là bắt buộc.");
            }

            // Xử lý geometry.coordinates
            var coordinatesJson = Request.Form["NewAsset.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    // Parse tọa độ từ chuỗi JSON
                    NewAsset.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Parsed coordinates: {JsonSerializer.Serialize(NewAsset.geometry.coordinates)}");

                    // Xóa lỗi ModelState cho coordinates nếu gán thành công
                    if (ModelState.ContainsKey("NewAsset.geometry.coordinates"))
                    {
                        ModelState["NewAsset.geometry.coordinates"].Errors.Clear();
                        ModelState["NewAsset.geometry.coordinates"].ValidationState = ModelValidationState.Valid;
                        Console.WriteLine("Cleared ModelState errors for NewAsset.geometry.coordinates");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewAsset.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                    Console.WriteLine($"Error parsing coordinates: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.geometry.coordinates", "Tọa độ là bắt buộc.");
                Console.WriteLine("Coordinates field is empty in form data");
            }

            //Xử lý geometrySystem
            var geometrySystem = Request.Form["GeometrySystem"];
            if (!string.IsNullOrEmpty(geometrySystem))
            {
                if (geometrySystem == "wgs84")
                {
                    Console.WriteLine($"Before convert wgs84 to vn2000, new coordinate:{JsonSerializer.Serialize(NewAsset.geometry.coordinates)}");
                    var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(NewAsset.geometry, 48);
                    NewAsset.geometry = vn2000Geometry;
                    Console.WriteLine($"After convert wgs84 to vn2000, new coordinate:{JsonSerializer.Serialize(NewAsset.geometry.coordinates)}");
                }
            }
            else
            {
                ModelState.AddModelError("GeometrySystem", "Phải chọn loại hệ thống địa lý");
            }

            // Xử lý attributes
            var attributesJson = Request.Form["NewAsset.attributes"];
            if (!string.IsNullOrEmpty(attributesJson))
            {
                try
                {
                    NewAsset.attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(attributesJson);
                    Console.WriteLine($"Parsed attributes: {JsonSerializer.Serialize(NewAsset.attributes)}");

                    // Xóa lỗi ModelState cho attributes nếu gán thành công
                    if (ModelState.ContainsKey("NewAsset.attributes"))
                    {
                        ModelState["NewAsset.attributes"].Errors.Clear();
                        ModelState["NewAsset.attributes"].ValidationState = ModelValidationState.Valid;
                        Console.WriteLine("Cleared ModelState errors for NewAsset.attributes");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewAsset.attributes", $"Định dạng thuộc tính không hợp lệ: {ex.Message}");
                    Console.WriteLine($"Error parsing attributes: {ex.Message}");
                }
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
                    Console.WriteLine("Asset");
                    Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                    return Page();
                }
                Console.WriteLine("Asset created successfully");
                return RedirectToPage("/Assets/Index");
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