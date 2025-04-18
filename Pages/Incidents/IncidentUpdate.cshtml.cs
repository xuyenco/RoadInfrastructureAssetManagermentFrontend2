using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentUpdateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;

        public IncidentUpdateModel(IIncidentsService incidentsService, IIncidentImageService incidentImageService)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
        }

        [BindProperty]
        public IncidentsRequest Incident { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public IFormFile[] Images { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            var incidentResponse = await _incidentsService.GetIncidentByIdAsync(id);
            if (incidentResponse == null)
            {
                TempData["Error"] = "Không tìm thấy Incident.";
                return NotFound();
            }

            Incident = new IncidentsRequest
            {
                address = incidentResponse.address,
                geometry = incidentResponse.geometry,
                route = incidentResponse.route,
                severity_level = incidentResponse.severity_level,
                damage_level = incidentResponse.damage_level,
                processing_status = incidentResponse.processing_status,
                task_id = incidentResponse.task_id
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Incident.geometry == null) Incident.geometry = new GeoJsonGeometry();

            var geometryType = Request.Form["Incident.geometry.type"];
            if (!string.IsNullOrEmpty(geometryType))
            {
                Incident.geometry.type = geometryType;
                ModelState["Incident.geometry.type"].Errors.Clear();
                ModelState["Incident.geometry.type"].ValidationState = ModelValidationState.Valid;
            }
            else
            {
                ModelState.AddModelError("Incident.geometry.type", "Loại hình học là bắt buộc.");
            }

            var coordinatesJson = Request.Form["Incident.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    Incident.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Raw coordinates: {JsonSerializer.Serialize(Incident.geometry.coordinates)}");

                    // Uncomment if VN2000 conversion is needed
                    if (GeometrySystem == "wgs84")
                    {
                        Incident.geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.geometry);
                        Console.WriteLine($"Converted to VN2000: {JsonSerializer.Serialize(Incident.geometry.coordinates)}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Incident.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("Incident.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            //if (!ModelState.IsValid)
            //{
            //    return Page();
            //}

            Console.WriteLine($"Data update:{JsonSerializer.Serialize(Incident.geometry)}");

            try
            {
                Console.WriteLine($"Updating incident: {JsonSerializer.Serialize(Incident)}");
                var updatedIncident = await _incidentsService.UpdateIncidentAsync(Id, Incident);
                if (updatedIncident == null)
                {
                    TempData["Error"] = "Cập nhật thất bại.";
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
                                incident_id = updatedIncident.incident_id,
                                image = image
                            };
                            var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                            if (createdImage == null)
                            {
                                Console.WriteLine($"Failed to create image for incident ID {updatedIncident.incident_id}");
                                TempData["Warning"] = "Incident đã được cập nhật, nhưng một số ảnh không thể thêm.";
                            }
                        }
                    }
                }

                TempData["Success"] = "Incident và ảnh (nếu có) đã được cập nhật thành công!";
                return RedirectToPage("/Incidents/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating incident: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Incident: {ex.Message}";
                return Page();
            }
        }
    }
}