using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentDetailModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;
        private readonly ILogger<IncidentDetailModel> _logger;

        public IncidentDetailModel(IIncidentsService incidentsService, IIncidentImageService incidentImageService, ILogger<IncidentDetailModel> logger)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
            _logger = logger;
        }

        public IncidentsResponse Incident { get; set; }
        public List<IncidentImageResponse> IncidentImages { get; set; } = new List<IncidentImageResponse>();
        public string LocationDisplay { get; set; } = "Không xác định";
        public GeoJsonGeometry Wgs84Geometry { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving details for incident with ID {IncidentId}",username, role, id);

            try
            {
                Incident = await _incidentsService.GetIncidentByIdAsync(id);
                if (Incident == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no incident with ID {IncidentId}",username, role, id);
                    TempData["Error"] = "Không tìm thấy Incident với ID này.";
                    return RedirectToPage("/Incidents/Index");
                }

                if (Incident.geometry != null && Incident.geometry.coordinates != null)
                {
                    // VN2000 coordinates for display
                    LocationDisplay = ParseCoordinates(Incident.geometry.type, Incident.geometry.coordinates);
                    _logger.LogDebug("User {Username} (Role: {Role}) parsed VN2000 coordinates for incident with ID {IncidentId}: {LocationDisplay}",username, role, id, LocationDisplay);

                    // Convert to WGS84 for map display
                    Wgs84Geometry = CoordinateConverter.ConvertGeometryToWGS84(Incident.geometry);
                    _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to WGS84 for incident with ID {IncidentId}: {Wgs84Geometry}",username, role, id, JsonSerializer.Serialize(Wgs84Geometry));
                }

                IncidentImages = await _incidentImageService.GetAllIncidentImagesByIncidentId(Incident.incident_id);
                if (IncidentImages == null)
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) found no images for incident with ID {IncidentId}",username, role, id);
                    IncidentImages = new List<IncidentImageResponse>();
                }
                else
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) retrieved {ImageCount} images for incident with ID {IncidentId}",username, role, id, IncidentImages.Count);
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully loaded details for incident with ID {IncidentId}",username, role, id);
                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access for incident with ID {IncidentId}: {Error}",username, role, id, ex.Message);
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading details for incident with ID {IncidentId}: {Error}",username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Incident: {ex.Message}";
                return RedirectToPage("/Incidents/Index");
            }
        }

        private string ParseCoordinates(string geometryType, object coordinates)
        {
            try
            {
                if (coordinates is JsonElement jsonElement)
                {
                    if (geometryType == "Point" && jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() == 2)
                    {
                        double x = jsonElement[0].GetDouble();
                        double y = jsonElement[1].GetDouble();
                        return $"[{x}, {y}]";
                    }
                    else if (geometryType == "LineString" && jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        var points = new List<string>();
                        foreach (var point in jsonElement.EnumerateArray())
                        {
                            if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() == 2)
                            {
                                double x = point[0].GetDouble();
                                double y = point[1].GetDouble();
                                points.Add($"[{x}, {y}]");
                            }
                        }
                        return $"[{string.Join(", ", points)}]";
                    }
                }
                else if (coordinates is double[] pointCoords && geometryType == "Point" && pointCoords.Length == 2)
                {
                    return $"[{pointCoords[0]}, {pointCoords[1]}]";
                }
                else if (coordinates is object[] lineCoords && geometryType == "LineString")
                {
                    var points = new List<string>();
                    foreach (var coord in lineCoords)
                    {
                        if (coord is double[] point && point.Length == 2)
                        {
                            points.Add($"[{point[0]}, {point[1]}]");
                        }
                    }
                    return $"[{string.Join(", ", points)}]";
                }

                _logger.LogWarning("Failed to parse coordinates: Type={GeometryType}, Coordinates={Coordinates}",geometryType, coordinates?.ToString());
                return $"Không xác định (Type: {geometryType}, Coordinates: {coordinates?.ToString()})";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error parsing coordinates: Type={GeometryType}, Error={Error}",geometryType, ex.Message);
                return $"Lỗi xử lý tọa độ: {ex.Message}";
            }
        }
    }
}