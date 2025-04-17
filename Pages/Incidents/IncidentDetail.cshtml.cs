using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend.Interface;
using Road_Infrastructure_Asset_Management.Model.Response;
using System.Text.Json;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using Road_Infrastructure_Asset_Management.Model.Geometry;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IncidentDetailModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;

        public IncidentDetailModel(IIncidentsService incidentsService, IIncidentImageService incidentImageService)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
        }

        public IncidentsResponse Incident { get; set; }
        public List<IncidentImageResponse> IncidentImages { get; set; } = new List<IncidentImageResponse>();
        public string LocationDisplay { get; set; } = "Không xác định";
        public GeoJsonGeometry Wgs84Geometry { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Incident = await _incidentsService.GetIncidentByIdAsync(id);
                if (Incident == null)
                {
                    TempData["Error"] = "Không tìm thấy Incident với ID này.";
                    return RedirectToPage("/Incidents/Index");
                }

                if (Incident.geometry != null && Incident.geometry.coordinates != null)
                {
                    // VN2000 coordinates for display
                    LocationDisplay = ParseCoordinates(Incident.geometry.type, Incident.geometry.coordinates);

                    // Convert to WGS84 for map display
                    Wgs84Geometry = CoordinateConverter.ConvertGeometryToWGS84(Incident.geometry);
                }

                Console.WriteLine($"Dữ liệu tọa độ cho bản đồ. \n Location Display : {LocationDisplay} \n Wgs84Geometry = {Wgs84Geometry}");

                IncidentImages = await _incidentImageService.GetAllIncidentImagesByIncidentId(Incident.incident_id);
                if (IncidentImages == null)
                {
                    IncidentImages = new List<IncidentImageResponse>();
                }

                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Unauthorized access: {ex.Message}");
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading incident details: {ex.Message}, StackTrace: {ex.StackTrace}");
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

                return $"Không xác định (Type: {geometryType}, Coordinates: {coordinates?.ToString()})";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing coordinates: {ex.Message}");
                return $"Lỗi xử lý tọa độ: {ex.Message}";
            }
        }
    }
}