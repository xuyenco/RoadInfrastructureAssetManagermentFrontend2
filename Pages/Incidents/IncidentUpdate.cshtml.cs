using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IncidentUpdateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;

        public IncidentUpdateModel(IIncidentsService incidentsService)
        {
            _incidentsService = incidentsService;
        }

        [BindProperty]
        public IncidentsRequest Incident { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; } // Nhận giá trị từ geometrySystemHidden

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            var incidentResponse = await _incidentsService.GetIncidentByIdAsync(id);
            Incident = new IncidentsRequest
            {
                asset_id = incidentResponse.asset_id,
                reported_by = incidentResponse.reported_by,
                incident_type = incidentResponse.incident_type,
                description = incidentResponse.description,
                location = incidentResponse.location, // Giữ nguyên VN2000
                priority = incidentResponse.priority,
                resolved_at = incidentResponse.resolved_at,
                status = incidentResponse.status,
                notes = incidentResponse.notes,
            };
            if (Incident == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Incident.location == null) Incident.location = new GeoJsonGeometry();

            // Xử lý location.type
            var geometryType = Request.Form["Incident.location.type"];
            if (!string.IsNullOrEmpty(geometryType))
            {
                Incident.location.type = geometryType;
                ModelState["Incident.location.type"].Errors.Clear();
                ModelState["Incident.location.type"].ValidationState = ModelValidationState.Valid;
            }
            else
            {
                ModelState.AddModelError("Incident.location.type", "Loại hình học là bắt buộc.");
            }

            // Xử lý location.coordinates
            var coordinatesJson = Request.Form["Incident.location.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    Incident.location.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Raw coordinates: {JsonSerializer.Serialize(Incident.location.coordinates)}");

                    // Chuyển đổi tọa độ dựa trên GeometrySystem
                    if (GeometrySystem == "wgs84")
                    {
                        Incident.location = CoordinateConverter.ConvertGeometryToVN2000(Incident.location);
                        Console.WriteLine($"Converted to VN2000: {JsonSerializer.Serialize(Incident.location.coordinates)}");
                    }
                    // Nếu GeometrySystem là "vn2000", giữ nguyên tọa độ

                    ModelState["Incident.location.coordinates"].Errors.Clear();
                    ModelState["Incident.location.coordinates"].ValidationState = ModelValidationState.Valid;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Incident.location.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("Incident.location.coordinates", "Tọa độ là bắt buộc.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var updatedIncident = await _incidentsService.UpdateIncidentAsync(Id, Incident);
            if (updatedIncident == null)
            {
                ModelState.AddModelError("", "Cập nhật thất bại.");
                return Page();
            }

            return RedirectToPage("/Incidents/Index");
        }
    }
}