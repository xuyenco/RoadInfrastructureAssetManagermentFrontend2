using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class AssetsDetailModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly ILogger<AssetsDetailModel> _logger;

        public AssetsDetailModel(IAssetsService assetsService, ILogger<AssetsDetailModel> logger)
        {
            _assetsService = assetsService;
            _logger = logger;
        }

        public AssetsResponse Asset { get; set; }
        public Dictionary<string, object> CustomAttributes { get; set; }
        public string LocationDisplay { get; set; } = "Không xác định";
        public GeoJsonGeometry Wgs84Geometry { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing asset detail for ID {AssetId}", username, role, id);

            try
            {
                Asset = await _assetsService.GetAssetByIdAsync(id);
                if (Asset == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no asset with ID {AssetId}", username, role, id);
                    TempData["Error"] = "Không tìm thấy tài sản với ID này.";
                    return Page();
                }

                CustomAttributes = Asset.custom_attributes ?? new Dictionary<string, object>();

                if (Asset.geometry != null && Asset.geometry.coordinates != null)
                {
                    // VN2000 coordinates for display
                    LocationDisplay = ParseCoordinates(Asset.geometry.type, Asset.geometry.coordinates);
                    _logger.LogDebug("User {Username} (Role: {Role}) parsed VN2000 coordinates for asset with ID {AssetId}: {LocationDisplay}", username, role, id, LocationDisplay);

                    // Convert to WGS84 for map display
                    Wgs84Geometry = CoordinateConverter.ConvertGeometryToWGS84(Asset.geometry);
                    _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to WGS84 for asset with ID {AssetId}: {Wgs84Geometry}", username, role, id, JsonSerializer.Serialize(Wgs84Geometry));
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved asset with ID {AssetId}", username, role, id);
                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access for asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error fetching asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = $"Lỗi khi tải chi tiết tài sản: {ex.Message}";
                return Page();
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
                    else if (geometryType == "Polygon" && jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        var rings = new List<string>();
                        foreach (var ring in jsonElement.EnumerateArray())
                        {
                            if (ring.ValueKind == JsonValueKind.Array)
                            {
                                var points = new List<string>();
                                foreach (var point in ring.EnumerateArray())
                                {
                                    if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() == 2)
                                    {
                                        double x = point[0].GetDouble();
                                        double y = point[1].GetDouble();
                                        points.Add($"[{x}, {y}]");
                                    }
                                }
                                rings.Add($"[{string.Join(", ", points)}]");
                            }
                        }
                        return $"[{string.Join(", ", rings)}]";
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
                else if (coordinates is object[] polygonCoords && geometryType == "Polygon")
                {
                    var rings = new List<string>();
                    foreach (var ring in polygonCoords)
                    {
                        if (ring is object[] ringCoords)
                        {
                            var points = new List<string>();
                            foreach (var coord in ringCoords)
                            {
                                if (coord is double[] point && point.Length == 2)
                                {
                                    points.Add($"[{point[0]}, {point[1]}]");
                                }
                            }
                            rings.Add($"[{string.Join(", ", points)}]");
                        }
                    }
                    return $"[{string.Join(", ", rings)}]";
                }

                _logger.LogWarning("Failed to parse coordinates: Type={GeometryType}, Coordinates={Coordinates}", geometryType, coordinates?.ToString());
                return $"Không xác định (Type: {geometryType}, Coordinates: {coordinates?.ToString()})";
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error parsing coordinates: Type={GeometryType}, Error={Error}", geometryType, ex.Message);
                return $"Lỗi xử lý tọa độ: {ex.Message}";
            }
        }
    }
}