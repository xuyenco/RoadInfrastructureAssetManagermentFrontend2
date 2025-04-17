using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using System.Text.Json;
using System.Threading.Tasks;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IncidentCreateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public IncidentCreateModel(IIncidentsService incidentsService, IIncidentImageService incidentImageService, IAssetCagetoriesService assetCagetoriesService)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public IncidentsRequest Incident { get; set; } = new IncidentsRequest();

        [BindProperty]
        public IFormFile[] Images { get; set; }

        public string GeometrySystem { get; set; }
        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var geometryType = Request.Form["Incident.geometry.type"].ToString();
            var coordinatesJson = Request.Form["Incident.geometry.coordinates"].ToString();
            var geometrySystem = Request.Form["GeometrySystem"].ToString();

            if (string.IsNullOrEmpty(geometryType))
            {
                ModelState.AddModelError("Incident.geometry.type", "Loại hình học là bắt buộc.");
            }
            else
            {
                Incident.geometry.type = geometryType;
            }

            if (string.IsNullOrEmpty(coordinatesJson))
            {
                ModelState.AddModelError("Incident.geometry.coordinates", "Tọa độ là bắt buộc.");
            }
            else
            {
                try
                {
                    Incident.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Deserialized coordinates: {JsonSerializer.Serialize(Incident.geometry.coordinates)}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    ModelState.AddModelError("Incident.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(geometrySystem))
            {
                ModelState.AddModelError("GeometrySystem", "Hệ tọa độ là bắt buộc.");
            }
            else
            {
                GeometrySystem = geometrySystem;
                if (geometrySystem == "wgs84")
                {
                    var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.geometry, 48);
                    Incident.geometry = vn2000Geometry;
                }
            }

            //if (!ModelState.IsValid)
            //{
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine($"Validation error: {error.ErrorMessage}");
            //    }
            //    AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            //    return Page();
            //}

            try
            {
                Console.WriteLine($"Creating incident: {JsonSerializer.Serialize(Incident)}");
                var createdIncident = await _incidentsService.CreateIncidentAsync(Incident);
                if (createdIncident == null)
                {
                    TempData["Error"] = "Không thể tạo Incident. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Incident creation failed: null response from service.");
                    //AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                    return Page();
                }

                if (Images != null && Images.Length > 0)
                {
                    foreach (var image in Images)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imageRequest = new IncidentImageRequest
                            {
                                incident_id = createdIncident.incident_id,
                                image = image
                            };
                            var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                            if (createdImage == null)
                            {
                                Console.WriteLine($"Failed to create image for incident ID {createdIncident.incident_id}");
                                TempData["Warning"] = "Incident đã được tạo, nhưng một số ảnh không thể thêm.";
                            }
                        }
                    }
                }

                TempData["Success"] = "Incident và ảnh (nếu có) đã được tạo thành công!";
                return RedirectToPage("/Incidents/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Incident: {ex.Message}";
                AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                return Page();
            }
        }
    }
}