using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TaskDetailModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public TaskDetailModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        public TasksResponse Task { get; set; }
        public string LocationDisplay { get; set; } = "Không xác định";
        public GeoJsonGeometry Wgs84Geometry { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Task = await _tasksService.GetTaskByIdAsync(id);
                if (Task == null)
                {
                    TempData["Error"] = "Không tìm thấy Nhiệm vụ với ID này.";
                    return RedirectToPage("/Tasks/Index");
                }

                if (Task.geometry != null && Task.geometry.coordinates != null)
                {
                    // VN2000 coordinates for display
                    LocationDisplay = ParseCoordinates(Task.geometry.type, Task.geometry.coordinates);

                    // Convert to WGS84 for map display
                    Wgs84Geometry = CoordinateConverter.ConvertGeometryToWGS84(Task.geometry);
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
                Console.WriteLine($"Error loading task details: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Nhiệm vụ: {ex.Message}";
                return RedirectToPage("/Tasks/Index");
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
