using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Assets
{
    public class AssetCreateModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public AssetCreateModel(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public AssetsRequest NewAsset { get; set; } = new AssetsRequest();

        public List<AssetCagetoriesResponse> Categories { get; set; } = new List<AssetCagetoriesResponse>();

        public async Task OnGetAsync()
        {
            Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
        }

        public async Task<IActionResult> OnGetGetCategorySchemaAsync(int id)
        {
            var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (category == null)
            {
                return Content("<p>Không tìm thấy danh mục.</p>");
            }

            var html = $@"
            <h3>Tạo tài sản cho: {category.cagetory_name}</h3>
            <div class='form-group'>
                <label>Loại hình học</label>
                <input type='text' class='form-control' value='{category.geometry_type}' disabled />
                <input type='hidden' id='geoType' value='{category.geometry_type}' />
            </div>
            <div id='coordinatesInput' class='form-group'></div>
            <h4>Thuộc tính</h4>";

            // Xử lý requiredFields
            List<object> requiredFields = new List<object>();
            if (category.attributes_schema != null && category.attributes_schema.TryGetValue("required", out var req) && req != null)
            {
                requiredFields = req as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(req));
            }

            // Xử lý properties
            var properties = category.attributes_schema != null && category.attributes_schema.TryGetValue("properties", out var props) && props != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(props))
                : new Dictionary<string, object>();

            foreach (var prop in properties)
            {
                var details = prop.Value != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(prop.Value))
                    : new Dictionary<string, object>();

                var isRequired = requiredFields.Contains(prop.Key);
                html += $@"
                <div class='form-group'>
                <label for='{prop.Key}'>{prop.Key} {(isRequired ? "(bắt buộc)" : "")}</label>";

                if (details.TryGetValue("enum", out var enumObj) && enumObj != null)
                {
                    var enumValues = enumObj as List<object> ?? JsonSerializer.Deserialize<List<object>>(JsonSerializer.Serialize(enumObj));
                    html += $"<select id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")}>";
                    html += "<option value=''>Không chọn</option>";
                    foreach (var val in enumValues)
                    {
                        html += $"<option value='{val}'>{val}</option>";
                    }
                    html += "</select>";
                }
                else
                {
                    html += $"<input type='text' id='{prop.Key}' name='{prop.Key}' class='form-control attribute-input' {(isRequired ? "required" : "")} />";
                }
                html += "</div>";
            }

            return Content(html, "text/html");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("Post method start - Request received");
            Console.WriteLine($"Form data: {JsonSerializer.Serialize(Request.Form)}");
            Console.WriteLine($"NewAsset before processing: {JsonSerializer.Serialize(NewAsset)}");

            if (NewAsset.geometry == null) NewAsset.geometry = new GeoJsonGeometry();

            var geometryType = Request.Form["NewAsset.Geometry.type"];
            if (!string.IsNullOrEmpty(geometryType))
            {
                NewAsset.geometry.type = geometryType;
            }
            else
            {
                ModelState.AddModelError("NewAsset.Geometry.type", "Geometry type is required.");
            }

            var coordinatesJson = Request.Form["NewAsset.Geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    NewAsset.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Deserialized coordinates: {JsonSerializer.Serialize(NewAsset.geometry.coordinates)}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewAsset.Geometry.coordinates", $"Invalid coordinates format: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.Geometry.coordinates", "Coordinates are required.");
            }

            var attributesJson = Request.Form["NewAsset.attributes"];
            if (!string.IsNullOrEmpty(attributesJson))
            {
                try
                {
                    NewAsset.attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(attributesJson);
                    Console.WriteLine($"Deserialized attributes: {JsonSerializer.Serialize(NewAsset.attributes)}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("NewAsset.attributes", $"Invalid attributes format: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("NewAsset.attributes", "Attributes are required.");
            }

            if (string.IsNullOrEmpty(NewAsset.condition))
            {
                ModelState.AddModelError("NewAsset.condition", "Condition is required.");
            }
            if (string.IsNullOrEmpty(NewAsset.lifecycle_stage))
            {
                ModelState.AddModelError("NewAsset.lifecycle_stage", "Lifecycle stage is required.");
            }

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

            Console.WriteLine("Post model pass");
            Console.WriteLine($"Request data sent to backend: {JsonSerializer.Serialize(NewAsset)}");

            try
            {
                var createdAsset = await _assetsService.CreateAssetAsync(NewAsset);
                if (createdAsset == null)
                {
                    ModelState.AddModelError("", "Failed to create asset.");
                    Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                    return Page();
                }
                return RedirectToPage("/Assets/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating asset: {ex.Message}");
                Categories = (await _assetCagetoriesService.GetAllAssetCagetoriesAsync()).ToList();
                return Page();
            }
        }
    }
}