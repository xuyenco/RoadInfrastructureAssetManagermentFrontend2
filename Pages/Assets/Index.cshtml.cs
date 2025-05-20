using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;
using System.Drawing;
using System.Reflection;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class IndexModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly IAssetsService _assetsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAssetCagetoriesService assetCagetoriesService, ILogger<IndexModel> logger, IAssetsService assetsService)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
            _assetsService = assetsService;
        }

        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing Asset Index page", username, role);
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting assets to Excel", username, role);

            try
            {
                // Fetch assets and categories
                _logger.LogDebug("User {Username} (Role: {Role}) fetching assets and categories", username, role);
                var assets = await _assetsService.GetAllAssetsAsync();
                var categories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();

                if (assets == null || !assets.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null or empty response when fetching assets for export", username, role);
                    return RedirectToPage();
                }

                if (categories == null || !categories.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null or empty response when fetching asset categories for export", username, role);
                    return RedirectToPage();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets and {CategoryCount} categories for export", username, role, assets.Count, categories.Count);

                // Set EPPlus license
                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    // Define header name mappings for category and asset properties
                    var categoryHeaderMap = new Dictionary<string, string>
                    {
                        { "category_id", "Mã loại tài sản" },
                        { "category_name", "Tên loại tài sản" },
                        { "geometry_type", "Loại hình học" },
                        { "created_at", "Ngày tạo loại tài sản" },
                        { "sample_image", "Hình ảnh mẫu" },
                        { "icon_url", "URL biểu tượng" }
                    };

                    var assetHeaderMap = new Dictionary<string, string>
                    {
                        { "asset_id", "Mã tài sản" },
                        { "asset_name", "Tên tài sản" },
                        { "asset_code", "Mã code" },
                        { "geometry", "Hình học" },
                        { "address", "Địa chỉ" },
                        { "construction_year", "Năm xây dựng" },
                        { "operation_year", "Năm vận hành" },
                        { "land_area", "Diện tích đất" },
                        { "floor_area", "Diện tích sàn" },
                        { "original_value", "Giá trị ban đầu" },
                        { "remaining_value", "Giá trị còn lại" },
                        { "asset_status", "Trạng thái" },
                        { "installation_unit", "Đơn vị lắp đặt" },
                        { "management_unit", "Đơn vị quản lý" },
                        { "created_at", "Ngày tạo tài sản" },
                        { "image_url", "URL hình ảnh" }
                    };

                    // Get properties of AssetCagetoriesResponse and AssetsResponse dynamically
                    var categoryProperties = typeof(AssetCagetoriesResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name != "attribute_schema") // Exclude attribute_schema as it’s handled separately
                        .ToList();
                    var assetProperties = typeof(AssetsResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name != "category_id" && p.Name != "custom_attributes") // Exclude category_id and custom_attributes
                        .ToList();

                    // Create category lookup for category_name
                    var categoryLookup = categories.ToDictionary(c => c.category_id, c => c.category_name);

                    // Group assets by category_id
                    var assetsByCategory = assets.GroupBy(a => a.category_id).ToDictionary(g => g.Key, g => g.ToList());

                    // Create a sheet for each category
                    foreach (var category in categories)
                    {
                        _logger.LogDebug("User {Username} (Role: {Role}) processing category {CategoryName} (ID: {CategoryId})", username, role, category.category_name, category.category_id);

                        // Sanitize category name for sheet name
                        var sheetName = category.category_name.Length > 31
                            ? category.category_name.Substring(0, 31).Replace(":", "").Replace("/", "").Replace("\\", "").Replace("?", "").Replace("*", "").Replace("[", "").Replace("]", "")
                            : category.category_name.Replace(":", "").Replace("/", "").Replace("\\", "").Replace("?", "").Replace("*", "").Replace("[", "").Replace("]", "");

                        // Create worksheet
                        var worksheet = package.Workbook.Worksheets.Add(string.IsNullOrEmpty(sheetName) ? $"Category_{category.category_id}" : sheetName);

                        // Get attributes from attribute_schema.properties
                        var attributeKeys = new List<string>();
                        if (category.attribute_schema != null && category.attribute_schema.TryGetValue("properties", out var props) && props != null)
                        {
                            try
                            {
                                var propertiesJson = JsonSerializer.Serialize(props);
                                var propertiesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
                                attributeKeys = propertiesDict?.Keys.OrderBy(k => k).ToList() ?? new List<string>();
                                _logger.LogDebug("User {Username} (Role: {Role}) extracted {AttributeCount} attribute keys for category {CategoryName}", username, role, attributeKeys.Count, category.category_name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to deserialize attribute_schema.properties for category {CategoryName}: {Error}", username, role, category.category_name, ex.Message);
                                attributeKeys = new List<string>();
                            }
                        }
                        else
                        {
                            _logger.LogDebug("User {Username} (Role: {Role}) found no properties in attribute_schema for category {CategoryName}", username, role, category.category_name);
                        }

                        // Create headers: category fields + category_name + asset fields + attribute keys
                        var headers = new List<string>();
                        headers.AddRange(categoryProperties.Select(p => categoryHeaderMap.ContainsKey(p.Name) ? categoryHeaderMap[p.Name] : p.Name));
                        headers.Add("Loại tài sản"); // For category_name
                        headers.AddRange(assetProperties.Select(p => assetHeaderMap.ContainsKey(p.Name) ? assetHeaderMap[p.Name] : p.Name));
                        headers.AddRange(attributeKeys);

                        // Add headers to worksheet
                        for (int i = 0; i < headers.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                            worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        }

                        // Get assets for this category
                        var categoryAssets = assetsByCategory.ContainsKey(category.category_id)
                            ? assetsByCategory[category.category_id]
                            : new List<AssetsResponse>();

                        _logger.LogDebug("User {Username} (Role: {Role}) found {AssetCount} assets for category {CategoryName}", username, role, categoryAssets.Count, category.category_name);

                        // Add data rows
                        int row = 2;
                        foreach (var asset in categoryAssets)
                        {
                            int col = 1;

                            // Add category fields
                            foreach (var prop in categoryProperties)
                            {
                                var value = prop.GetValue(category);
                                worksheet.Cells[row, col].Value = value != null
                                    ? (value is DateTime dt ? dt.ToString("dd/MM/yyyy HH:mm") : value.ToString())
                                    : "Chưa có dữ liệu";
                                col++;
                            }

                            // Add category_name
                            worksheet.Cells[row, col].Value = category.category_name;
                            col++;

                            // Add asset fields
                            foreach (var prop in assetProperties)
                            {
                                var value = prop.GetValue(asset);
                                if (prop.Name == "geometry" && value != null)
                                {
                                    var geo = (GeoJsonGeometry)value;
                                    worksheet.Cells[row, col].Value = geo != null
                                        ? $"{geo.type}: {JsonSerializer.Serialize(geo.coordinates)}"
                                        : "Chưa có dữ liệu";
                                }
                                else
                                {
                                    worksheet.Cells[row, col].Value = value != null
                                        ? (value is DateTime dt ? dt.ToString("dd/MM/yyyy") : value.ToString())
                                        : "Chưa có dữ liệu";
                                }
                                col++;
                            }

                            // Add custom attributes based on attribute_schema
                            foreach (var key in attributeKeys)
                            {
                                worksheet.Cells[row, col].Value = asset.custom_attributes != null && asset.custom_attributes.ContainsKey(key)
                                    ? asset.custom_attributes[key]?.ToString()
                                    : "Chưa có dữ liệu";
                                col++;
                            }

                            row++;
                        }

                        // Auto-fit columns
                        worksheet.Cells.AutoFitColumns();
                    }

                    // Create file stream
                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Assets_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {AssetCount} assets across {CategoryCount} sheets to Excel file {FileName}", username, role, assets.Count, categories.Count, fileName);
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting assets to Excel: {Error}", username, role, ex.Message);
                return RedirectToPage();
            }
        }
    }
}