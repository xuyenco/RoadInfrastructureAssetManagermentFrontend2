using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System.Reflection;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class AssetsTableModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IReportService _reportService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetsTableModel> _logger;

        public AssetsTableModel(IAssetsService assetsService, IReportService reportService, IAssetCagetoriesService assetCagetoriesService, ILogger<AssetsTableModel> logger)
        {
            _assetsService = assetsService;
            _reportService = reportService;
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        public List<AssetCagetoriesResponse> assetCagetoriesResponses { get; set; } = new List<AssetCagetoriesResponse>();
        public List<AssetStatusReport> AssetStatusReport { get; set; } = new List<AssetStatusReport>();

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the assets table page", username, role);

            try
            {

                var result = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();
                assetCagetoriesResponses = result?.ToList() ?? new List<AssetCagetoriesResponse>();
                AssetStatusReport = await _reportService.GetAssetDistributedByCondition();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading asset status report: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi tải dữ liệu báo cáo: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetAssetsAsync(int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is fetching assets - Page: {CurrentPage}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, currentPage, pageSize, searchTerm, searchField);

            try
            {
                var (assets, totalCount) = await _assetsService.GetAssetsAsync(currentPage, pageSize, searchTerm, searchField);
                if (assets == null || !assets.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received empty assets list for Page: {CurrentPage}", username, role, currentPage);
                    return new JsonResult(new { success = true, assets = new List<AssetsResponse>(), totalCount = 0 });
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets with total count {TotalCount} for Page: {CurrentPage}",
                    username, role, assets.Count, totalCount, currentPage);
                return new JsonResult(new { success = true, assets, totalCount });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access while fetching assets", username, role);
                HttpContext.Session.Clear();
                return new JsonResult(new { success = false, message = "Phiên làm việc hết hạn, vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error fetching assets: {Error}", username, role, ex.Message);
                return new JsonResult(new { success = false, message = $"Lỗi khi tải danh sách tài sản: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete asset with ID {AssetId}", username, role, id);

            try
            {
                var deleted = await _assetsService.DeleteAssetAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete asset with ID {AssetId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Xóa tài sản thất bại." });
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted asset with ID {AssetId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi khi xóa tài sản: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting all assets to Excel", username, role);

            try
            {
                // Fetch assets and categories
                _logger.LogDebug("User {Username} (Role: {Role}) fetching assets and categories", username, role);
                var assets = await _assetsService.GetAllAssetsAsync();
                var categories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();

                if (assets == null || !assets.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null or empty response when fetching assets for export", username, role);
                    TempData["Error"] = "Không tìm thấy tài sản nào để xuất báo cáo.";
                    return RedirectToPage();
                }

                if (categories == null || !categories.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null or empty response when fetching asset categories for export", username, role);
                    TempData["Error"] = "Không tìm thấy danh mục nào để xuất báo cáo.";
                    return RedirectToPage();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets and {CategoryCount} categories for export", username, role, assets.Count, categories.Count);

                // Set EPPlus license
                ExcelPackage.License.SetNonCommercialPersonal("Duong");

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
                        .Where(p => p.Name != "attribute_schema")
                        .ToList();
                    var assetProperties = typeof(AssetsResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.Name != "category_id" && p.Name != "custom_attributes")
                        .ToList();

                    // Group assets by category_id
                    var assetsByCategory = assets.GroupBy(a => a.category_id).ToDictionary(g => g.Key, g => g.ToList());

                    // Create a sheet for each category
                    foreach (var category in categories)
                    {
                        _logger.LogDebug("Processing category {CategoryName} (ID: {CategoryId})", category.category_name, category.category_id);

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
                                _logger.LogDebug("Extracted {AttributeCount} attribute keys for category {CategoryName}", attributeKeys.Count, category.category_name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Failed to deserialize attribute_schema.properties for category {CategoryName}: {Error}", category.category_name, ex.Message);
                                attributeKeys = new List<string>();
                            }
                        }

                        // Create headers: category fields + category_name + asset fields + attribute keys
                        var headers = new List<string>();
                        headers.AddRange(categoryProperties.Select(p => categoryHeaderMap.ContainsKey(p.Name) ? categoryHeaderMap[p.Name] : p.Name));
                        headers.Add("Loại tài sản");
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

                        _logger.LogDebug("Found {AssetCount} assets for category {CategoryName}", categoryAssets.Count, category.category_name);

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
                                    : "";
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
                                        : "";
                                }
                                else
                                {
                                    worksheet.Cells[row, col].Value = value != null
                                        ? (value is DateTime dt ? dt.ToString("dd/MM/yyyy") : value.ToString())
                                        : "";
                                }
                                col++;
                            }

                            // Add custom attributes based on attribute_schema
                            foreach (var key in attributeKeys)
                            {
                                worksheet.Cells[row, col].Value = asset.custom_attributes != null && asset.custom_attributes.ContainsKey(key)
                                    ? asset.custom_attributes[key]?.ToString()
                                    : "";
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
                TempData["Error"] = $"Lỗi khi xuất báo cáo tài sản: {ex.Message}";
                return RedirectToPage();
            }
        }
        public async Task<IActionResult> OnGetExportAssetsByCategoryIdAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting assets for Category ID {CategoryId} to Excel", username, role, id);

            try
            {
                var assets = await _assetsService.GetAssetsByCategoryIdAsync(id);
                if (assets == null || !assets.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null or empty response when fetching assets for Category ID {CategoryId}", username, role, id);
                    TempData["Error"] = $"Không tìm thấy tài sản nào cho danh mục {id}.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets for Category ID {CategoryId}", username, role, assets.Count, id);

                ExcelPackage.License.SetNonCommercialPersonal("Duong");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add($"Assets_Category_{id}");

                    // Thu thập tất cả các key từ custom_attributes
                    var attributeKeys = new HashSet<string>();
                    foreach (var asset in assets)
                    {
                        if (asset.custom_attributes != null)
                        {
                            foreach (var key in asset.custom_attributes.Keys)
                            {
                                attributeKeys.Add(key);
                            }
                        }
                    }
                    var attributeKeysList = attributeKeys.OrderBy(k => k).ToList();

                    // Tạo header: cột tĩnh + cột động từ custom_attributes
                    var staticHeaders = new[]
                    {
                        "Mã tài sản",
                        "Tên tài sản",
                        "Mã số tài sản",
                        "Địa chỉ",
                        "Trạng thái",
                        "Ngày tạo",
                        "Hình ảnh",
                        "Danh mục ID",
                        "Năm xây dựng",
                        "Năm vận hành",
                        "Diện tích đất",
                        "Diện tích sàn",
                        "Giá trị ban đầu",
                        "Giá trị còn lại",
                        "Đơn vị lắp đặt",
                        "Đơn vị quản lý"
                    };
                    var headers = staticHeaders.Concat(attributeKeysList).ToArray();

                    // Ghi header
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    // Ghi dữ liệu
                    int row = 2;
                    foreach (var asset in assets)
                    {
                        // Cột tĩnh
                        worksheet.Cells[row, 1].Value = asset.asset_id;
                        worksheet.Cells[row, 2].Value = asset.asset_name;
                        worksheet.Cells[row, 3].Value = asset.asset_code;
                        worksheet.Cells[row, 4].Value = asset.address;
                        worksheet.Cells[row, 5].Value = asset.asset_status;
                        worksheet.Cells[row, 6].Value = asset.created_at.HasValue
                            ? asset.created_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 7].Value = asset.image_url ?? "Không có hình ảnh";
                        worksheet.Cells[row, 8].Value = asset.category_id;
                        worksheet.Cells[row, 9].Value = asset.construction_year.HasValue
                            ? asset.construction_year.Value.ToString("dd/MM/yyyy")
                            : "";
                        worksheet.Cells[row, 10].Value = asset.operation_year.HasValue
                            ? asset.operation_year.Value.ToString("dd/MM/yyyy")
                            : "";
                        worksheet.Cells[row, 11].Value = asset.land_area.HasValue ? asset.land_area.Value : (object)"";
                        worksheet.Cells[row, 12].Value = asset.floor_area.HasValue ? asset.floor_area.Value : (object)"";
                        worksheet.Cells[row, 13].Value = asset.original_value.HasValue ? asset.original_value.Value : (object)"";
                        worksheet.Cells[row, 14].Value = asset.remaining_value.HasValue ? asset.remaining_value.Value : (object)"";
                        worksheet.Cells[row, 15].Value = asset.installation_unit ?? "";
                        worksheet.Cells[row, 16].Value = asset.management_unit ?? "";

                        // Cột động (custom_attributes)
                        if (asset.custom_attributes != null)
                        {
                            for (int i = 0; i < attributeKeysList.Count; i++)
                            {
                                var key = attributeKeysList[i];
                                worksheet.Cells[row, 17 + i].Value = asset.custom_attributes.TryGetValue(key, out var value) ? value?.ToString() : "";
                            }
                        }

                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"AssetsByCategory_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {AssetCount} assets for Category ID {CategoryId} to Excel file {FileName}",
                        username, role, assets.Count, id, fileName);
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting assets for Category ID {CategoryId} to Excel: {Error}",
                    username, role, id, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo tài sản cho danh mục {id}: {ex.Message}";
                return RedirectToPage();
            }
        }

    }
}