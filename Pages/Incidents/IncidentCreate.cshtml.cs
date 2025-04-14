using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Model.Request;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new(); // Thêm danh sách categories

        public async Task<IActionResult> OnGetAsync()
        {
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Xử lý location.type và coordinates từ form
            var locationType = Request.Form["Incident.location.type"].ToString();
            var coordinatesJson = Request.Form["Incident.location.coordinates"].ToString();
            var geometrySystem = Request.Form["GeometrySystem"].ToString();

            if (string.IsNullOrEmpty(locationType))
            {
                ModelState.AddModelError("Incident.location.type", "Loại hình học là bắt buộc.");
            }
            else
            {
                Incident.location.type = locationType;
            }

            if (string.IsNullOrEmpty(coordinatesJson))
            {
                ModelState.AddModelError("Incident.location.coordinates", "Tọa độ là bắt buộc.");
            }
            else
            {
                try
                {
                    Incident.location.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Deserialized coordinates: {JsonSerializer.Serialize(Incident.location.coordinates)}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    ModelState.AddModelError("Incident.location.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(geometrySystem))
            {
                ModelState.AddModelError("GeometrySystem", "Hệ tọa độ là bắt buộc.");
            }
            else
            {
                GeometrySystem = geometrySystem;
                //Giả định có CoordinateConverter như trong AssetCreate2
                //Nếu cần chuyển đổi sang VN2000, uncomment và thêm CoordinateConverter
                if (geometrySystem == "wgs84")
                {
                    var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.location, 48);
                    Incident.location = vn2000Geometry;
                }
            }
            Console.WriteLine($"Data output for incident:{JsonSerializer.Serialize(Incident)}");  
            // 2. Kiểm tra ModelState
            //if (!ModelState.IsValid)
            //{
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine($"Validation error: {error.ErrorMessage}");
            //    }
            //    return Page();
            //}

            // 3. Gửi yêu cầu tạo Incident
            try
            {
                Console.WriteLine($"Creating incident with Asset ID: {Incident.asset_id}, Type: {Incident.incident_type}");
                var createdIncident = await _incidentsService.CreateIncidentAsync(Incident);
                if (createdIncident == null)
                {
                    TempData["Error"] = "Không thể tạo Incident. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Incident creation failed: null response from service.");
                    return Page();
                }

                // 4. Xử lý thêm ảnh nếu có
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
                return Page();
            }
        }
    }
}