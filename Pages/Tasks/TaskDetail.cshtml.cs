using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TaskDetailModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly ILogger<TaskDetailModel> _logger;

        public TaskDetailModel(ITasksService tasksService, ILogger<TaskDetailModel> logger)
        {
            _tasksService = tasksService;
            _logger = logger;
        }

        public TasksResponse Task { get; set; }
        public string LocationDisplay { get; set; } = "Không xác định";
        public GeoJsonGeometry Wgs84Geometry { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing details for task with ID {TaskId}", username, role, id);

            try
            {
                Task = await _tasksService.GetTaskByIdAsync(id);
                if (Task == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId}", username, role, id);
                    TempData["Error"] = "Không tìm thấy Nhiệm vụ với ID này.";
                    return RedirectToPage("/Tasks/Index");
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved task with ID {TaskId}", username, role, id);

                if (Task.geometry != null && Task.geometry.coordinates != null)
                {
                    // VN2000 coordinates for display
                    LocationDisplay = ParseCoordinates(Task.geometry.type, Task.geometry.coordinates);
                    _logger.LogDebug("User {Username} (Role: {Role}) parsed VN2000 coordinates for task ID {TaskId}: {LocationDisplay}", username, role, id, LocationDisplay);

                    // Convert to WGS84 for map display
                    Wgs84Geometry = CoordinateConverter.ConvertGeometryToWGS84(Task.geometry);
                    _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to WGS84 for task ID {TaskId}: {Wgs84Geometry}", username, role, id, JsonSerializer.Serialize(Wgs84Geometry));
                }
                else
                {
                    _logger.LogDebug("User {Username} (Role: {Role}) found no geometry data for task ID {TaskId}", username, role, id);
                }

                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access for task ID {TaskId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading task details for task ID {TaskId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Nhiệm vụ: {ex.Message}";
                return RedirectToPage("/Tasks/Index");
            }
        }

        private string ParseCoordinates(string geometryType, object coordinates)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogDebug("User {Username} (Role: {Role}) is parsing coordinates for geometry type {GeometryType}: {Coordinates}", username, role, geometryType, JsonSerializer.Serialize(coordinates));

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

                _logger.LogWarning("User {Username} (Role: {Role}) could not parse coordinates for geometry type {GeometryType}: {Coordinates}", username, role, geometryType, JsonSerializer.Serialize(coordinates));
                return $"Không xác định (Type: {geometryType}, Coordinates: {JsonSerializer.Serialize(coordinates)})";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered error parsing coordinates for geometry type {GeometryType}: {Error}", username, role, geometryType, ex.Message);
                return $"Lỗi xử lý tọa độ: {ex.Message}";
            }
        }
    }
}