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
    public class AssetUpdateModel : PageModel
    {
        private readonly IAssetsService _assetsService;

        public AssetUpdateModel(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        public AssetsResponse AssetResponse { get; set; }

        [BindProperty]
        public AssetsRequest AssetRequest { get; set; }

        [BindProperty(SupportsGet = true)]
        public int AssetId { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; } // Nhận giá trị từ geometrySystemHidden

        public async Task<IActionResult> OnGetAsync(int id)
        {
            AssetId = id;
            Console.WriteLine($"OnGetAsync - AssetId: {AssetId}");
            AssetResponse = await _assetsService.GetAssetByIdAsync(id);
            if (AssetResponse == null)
            {
                return NotFound();
            }

            AssetRequest = new AssetsRequest
            {
                cagetory_id = AssetResponse.cagetory_id,
                geometry = AssetResponse.geometry ?? new GeoJsonGeometry(), // Giữ nguyên VN2000
                attributes = AssetResponse.attributes ?? new Dictionary<string, object>(),
                lifecycle_stage = AssetResponse.lifecycle_stage,
                installation_date = AssetResponse.installation_date,
                expected_lifetime = AssetResponse.expected_lifetime,
                condition = AssetResponse.condition,
                last_inspection_date = AssetResponse.last_inspection_date
            };

            Console.WriteLine($"Data: {JsonSerializer.Serialize(AssetRequest)}");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine($"OnPostAsync - AssetId: {AssetId}");

            if (AssetRequest.geometry == null) AssetRequest.geometry = new GeoJsonGeometry();

            // Xử lý geometry.type
            var geometryType = Request.Form["AssetRequest.geometry.type"];
            Console.WriteLine("GeometryType: " + geometryType);
            if (!string.IsNullOrEmpty(geometryType))
            {
                AssetRequest.geometry.type = geometryType;
                if (ModelState.ContainsKey("AssetRequest.geometry.type"))
                {
                    ModelState["AssetRequest.geometry.type"].Errors.Clear();
                    ModelState["AssetRequest.geometry.type"].ValidationState = ModelValidationState.Valid;
                }
            }
            else
            {
                ModelState.AddModelError("AssetRequest.geometry.type", "Loại hình học là bắt buộc.");
            }

            // Xử lý geometry.coordinates
            var coordinatesJson = Request.Form["AssetRequest.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    AssetRequest.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Raw coordinates: {JsonSerializer.Serialize(AssetRequest.geometry.coordinates)}");

                    // Chuyển đổi tọa độ dựa trên GeometrySystem
                    if (GeometrySystem == "wgs84")
                    {
                        AssetRequest.geometry = CoordinateConverter.ConvertGeometryToVN2000(AssetRequest.geometry, 48);
                        Console.WriteLine($"Converted to VN2000: {JsonSerializer.Serialize(AssetRequest.geometry.coordinates)}");
                    }
                    // Nếu GeometrySystem là "vn2000", giữ nguyên tọa độ

                    if (ModelState.ContainsKey("AssetRequest.geometry.coordinates"))
                    {
                        ModelState["AssetRequest.geometry.coordinates"].Errors.Clear();
                        ModelState["AssetRequest.geometry.coordinates"].ValidationState = ModelValidationState.Valid;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("AssetRequest.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("AssetRequest.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            // Xử lý attributes từ form
            AssetRequest.attributes = new Dictionary<string, object>();
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("AssetRequest.attributes[")))
            {
                var attributeKey = key.Replace("AssetRequest.attributes[", "").TrimEnd(']');
                AssetRequest.attributes[attributeKey] = Request.Form[key].ToString();
                Console.WriteLine($"Parsed attributes: {JsonSerializer.Serialize(AssetRequest.attributes)}");
            }
            if (AssetRequest.attributes.Any() && ModelState.ContainsKey("AssetRequest.attributes"))
            {
                ModelState["AssetRequest.attributes"].Errors.Clear();
                ModelState["AssetRequest.attributes"].ValidationState = ModelValidationState.Valid;
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                if (AssetResponse == null)
                {
                    return NotFound();
                }
                return Page();
            }

            var updatedAsset = await _assetsService.UpdateAssetAsync(AssetId, AssetRequest);
            if (updatedAsset == null)
            {
                ModelState.AddModelError("", "Cập nhật thất bại.");
                AssetResponse = await _assetsService.GetAssetByIdAsync(AssetId);
                if (AssetResponse == null)
                {
                    return NotFound();
                }
                return Page();
            }

            return RedirectToPage("/Assets/Index");
        }
    }
}