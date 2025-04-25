using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentUpdateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;
        private readonly ILogger<IncidentUpdateModel> _logger;

        public IncidentUpdateModel(IIncidentsService incidentsService, IIncidentImageService incidentImageService, ILogger<IncidentUpdateModel> logger)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
            _logger = logger;
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
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving incident with ID {IncidentId} for update",username, role, id);

            Id = id;
            var incidentResponse = await _incidentsService.GetIncidentByIdAsync(id);
            if (incidentResponse == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no incident with ID {IncidentId}",username, role, id);
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

            _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved incident with ID {IncidentId}",username, role, id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting update for incident with ID {IncidentId}",username, role, Id);

            if (Incident.geometry == null)
            {
                Incident.geometry = new GeoJsonGeometry();
                _logger.LogDebug("User {Username} (Role: {Role}) initialized empty geometry for incident with ID {IncidentId}",username, role, Id);
            }

            var geometryType = Request.Form["Incident.geometry.type"];
            if (!string.IsNullOrEmpty(geometryType))
            {
                Incident.geometry.type = geometryType;
                if (ModelState.ContainsKey("Incident.geometry.type"))
                {
                    ModelState["Incident.geometry.type"].Errors.Clear();
                    ModelState["Incident.geometry.type"].ValidationState = ModelValidationState.Valid;
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry type for incident with ID {IncidentId}",username, role, Id);
                ModelState.AddModelError("Incident.geometry.type", "Loại hình học là bắt buộc.");
            }

            var coordinatesJson = Request.Form["Incident.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    Incident.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    _logger.LogDebug("User {Username} (Role: {Role}) provided raw coordinates for incident with ID {IncidentId}: {Coordinates}",username, role, Id, JsonSerializer.Serialize(Incident.geometry.coordinates));

                    if (GeometrySystem == "wgs84")
                    {
                        Incident.geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.geometry);
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for incident with ID {IncidentId}: {Coordinates}",username, role, Id, JsonSerializer.Serialize(Incident.geometry.coordinates));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid coordinates for incident with ID {IncidentId}: {Error}",username, role, Id, ex.Message);
                    ModelState.AddModelError("Incident.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide coordinates for incident with ID {IncidentId}",username, role, Id);
                ModelState.AddModelError("Incident.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors for incident with ID {IncidentId}: {Errors}",username, role, Id, JsonSerializer.Serialize(errors));
                return Page();
            }

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) sending update data for incident with ID {IncidentId}: {IncidentData}",username, role, Id, JsonSerializer.Serialize(Incident));
                var updatedIncident = await _incidentsService.UpdateIncidentAsync(Id, Incident);
                if (updatedIncident == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to update incident with ID {IncidentId}: No result returned",username, role, Id);
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
                            _logger.LogDebug("User {Username} (Role: {Role}) uploading image for incident with ID {IncidentId}: filename={FileName}, size={Size}",username, role, Id, image.FileName, image.Length);
                            var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                            if (createdImage == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to upload image for incident with ID {IncidentId}: filename={FileName}",username, role, Id, image.FileName);
                                TempData["Warning"] = "Incident đã được cập nhật, nhưng một số ảnh không thể thêm.";
                            }
                        }
                    }
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully updated incident with ID {IncidentId} and uploaded {ImageCount} images",username, role, Id, Images?.Length ?? 0);
                TempData["Success"] = "Incident và ảnh (nếu có) đã được cập nhật thành công!";
                return RedirectToPage("/Incidents/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error updating incident with ID {IncidentId}: {Error}",username, role, Id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Incident: {ex.Message}";
                return Page();
            }
        }
    }
}