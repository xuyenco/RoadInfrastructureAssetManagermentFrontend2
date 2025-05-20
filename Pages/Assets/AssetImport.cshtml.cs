using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Filter;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class AssetImportModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetImportModel> _logger;

        public AssetImportModel(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService, ILogger<AssetImportModel> logger)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        public List<AssetCagetoriesResponse> Categories { get; set; }

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the asset import page", username, role);
            Categories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {CategoryCount} categories for import",
                username, role, Categories.Count);
        }

        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync(int categoryId)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for category ID {CategoryId}",
                username, role, categoryId);

            if (categoryId == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not select a category for template download",
                    username, role);
                return BadRequest("Vui lòng chọn danh mục");
            }

            var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(categoryId);
            if (category == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no category with ID {CategoryId}",
                    username, role, categoryId);
                return NotFound("Category không tồn tại");
            }

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Asset template for {category.category_name}");
                var fixedColumns = new List<string>
                {
                    "category_id", "asset_name", "asset_code", "address", "geometry_coordinates", "geometry_system",
                    "construction_year", "operation_year", "land_area", "floor_area", "original_value", "remaining_value",
                    "asset_status", "installation_unit", "management_unit", "image_path"
                };

                var properties = category.attribute_schema != null &&
                                category.attribute_schema.TryGetValue("properties", out var props) &&
                                props != null
                                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(props))
                                : new Dictionary<string, object>();
                var attributeColumns = properties.Keys.ToList();

                for (int i = 0; i < fixedColumns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = fixedColumns[i];
                }
                for (int i = 0; i < attributeColumns.Count; i++)
                {
                    worksheet.Cells[1, fixedColumns.Count + i + 1].Value = attributeColumns[i];
                }
                worksheet.Cells[2, 1].Value = categoryId;

                worksheet.Cells[1, 1, 1, fixedColumns.Count + attributeColumns.Count].Style.Font.Bold = true;
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for category ID {CategoryId}",
                    username, role, categoryId);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Asset_Template_Category_{categoryId}.xlsx");
            }
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile, IFormFileCollection imageFiles)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is importing assets from Excel file", username, role);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not upload an Excel file", username, role);
                return BadRequest("Vui lòng chọn file Excel.");
            }

            try
            {
                var assets = new List<AssetsRequest>();
                var errorRows = new List<ExcelErrorRow>();
                var imageFileMap = imageFiles.ToDictionary(f => f.FileName, f => f);

                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        var colCount = worksheet.Dimension?.Columns ?? 0;

                        if (rowCount < 2 || colCount < 1)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) uploaded an empty or invalid Excel file", username, role);
                            return BadRequest("File Excel trống hoặc không hợp lệ.");
                        }

                        var headers = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Text?.ToLower() ?? "");
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var asset = new AssetsRequest { custom_attributes = "{}", geometry = "" };
                            var rowData = new Dictionary<string, string>();
                            string geometryCoordinates = null;
                            string geometrySystem = null;
                            string imagePath = null;

                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "category_id":
                                        asset.category_id = int.TryParse(value, out var catId) ? catId : 0; break;
                                    case "asset_name":
                                        asset.asset_name = value; break;
                                    case "asset_code":
                                        asset.asset_code = value; break;
                                    case "address":
                                        asset.address = value; break;
                                    case "geometry_coordinates":
                                        geometryCoordinates = value; break;
                                    case "geometry_system":
                                        geometrySystem = value; break;
                                    case "construction_year":
                                        asset.construction_year = DateTime.TryParse(value, out var constYear) ? constYear : null; break;
                                    case "operation_year":
                                        asset.operation_year = DateTime.TryParse(value, out var opYear) ? opYear : null; break;
                                    case "land_area":
                                        asset.land_area = double.TryParse(value, out var landArea) ? landArea : null; break;
                                    case "floor_area":
                                        asset.floor_area = double.TryParse(value, out var floorArea) ? floorArea : null; break;
                                    case "original_value":
                                        asset.original_value = double.TryParse(value, out var origValue) ? origValue : null; break;
                                    case "remaining_value":
                                        asset.remaining_value = double.TryParse(value, out var remValue) ? remValue : null; break;
                                    case "asset_status":
                                        asset.asset_status = value; break;
                                    case "installation_unit":
                                        asset.installation_unit = value; break;
                                    case "management_unit":
                                        asset.management_unit = value; break;
                                    case "image_path":
                                        imagePath = value;
                                        if (!string.IsNullOrEmpty(value) && imageFileMap.ContainsKey(Path.GetFileName(value)))
                                        {
                                            asset.image = imageFileMap[Path.GetFileName(value)];
                                            _logger.LogDebug("User {Username} (Role: {Role}) matched image '{ImagePath}' for row {Row}",
                                                username, role, value, row);
                                        }
                                        break;
                                    default:
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            var attributes = JsonSerializer.Deserialize<Dictionary<string, object>>(asset.custom_attributes);
                                            attributes[header] = value;
                                            asset.custom_attributes = JsonSerializer.Serialize(attributes);
                                        }
                                        break;
                                }
                            }

                            // Xử lý geometry_coordinates dưới dạng GeoJSON
                            if (!string.IsNullOrEmpty(geometryCoordinates))
                            {
                                try
                                {
                                    // Kiểm tra định dạng GeoJSON
                                    var geometryJson = JsonSerializer.Deserialize<JsonElement>(geometryCoordinates);
                                    if (geometryJson.TryGetProperty("type", out var typeElement) &&
                                        geometryJson.TryGetProperty("coordinates", out var coordsElement))
                                    {
                                        asset.geometry = geometryCoordinates;

                                        // Chuyển đổi tọa độ nếu geometry_system là WGS84
                                        if (geometrySystem?.ToUpper() == "WGS84")
                                        {
                                            var geoJsonGeometry = JsonSerializer.Deserialize<GeoJsonGeometry>(asset.geometry);
                                            var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(geoJsonGeometry, 48);
                                            asset.geometry = JsonSerializer.Serialize(vn2000Geometry);
                                            _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for row {Row}: {Geometry}",
                                                username, role, row, asset.geometry);
                                        }
                                    }
                                    else
                                    {
                                        throw new JsonException("GeoJSON thiếu trường 'type' hoặc 'coordinates'.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("User {Username} (Role: {Role}) encountered geometry error in row {Row}: {Error}",
                                        username, role, row, ex.Message);
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = row,
                                        OriginalData = JsonSerializer.Serialize(rowData),
                                        ErrorMessage = $"Lỗi khi xử lý geometry: {ex.Message}"
                                    });
                                    continue;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) missing geometry_coordinates in row {Row}",
                                    username, role, row);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = "Thiếu geometry_coordinates."
                                });
                                continue;
                            }

                            // Kiểm tra ảnh
                            if (!string.IsNullOrEmpty(imagePath) && !imageFileMap.ContainsKey(Path.GetFileName(imagePath)))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) did not find image '{ImagePath}' for row {Row}",
                                    username, role, imagePath, row);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = $"Không tìm thấy ảnh '{imagePath}' trong số ảnh tải lên."
                                });
                                continue;
                            }

                            assets.Add(asset);
                        }

                        int successCount = 0;
                        for (int i = 0; i < assets.Count; i++)
                        {
                            var asset = assets[i];
                            var rowNumber = i + 2;

                            if (asset.category_id == 0 || string.IsNullOrEmpty(asset.asset_name) || string.IsNullOrEmpty(asset.geometry) || string.IsNullOrEmpty(asset.custom_attributes))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid data in row {Row}: Missing category_id, asset_name, geometry, or custom_attributes",
                                    username, role, rowNumber);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(asset),
                                    ErrorMessage = "Dữ liệu không hợp lệ: Thiếu category_id, asset_name, geometry, hoặc custom_attributes."
                                });
                                continue;
                            }

                            try
                            {
                                _logger.LogDebug("User {Username} (Role: {Role}) creating asset for row {Row}: {AssetData}",
                                    username, role, rowNumber, JsonSerializer.Serialize(asset));
                                var createdAsset = await _assetsService.CreateAssetAsync(asset);
                                if (createdAsset == null)
                                {
                                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create asset for row {Row}: No result returned",
                                        username, role, rowNumber);
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(asset),
                                        ErrorMessage = "Không thể tạo tài sản (lỗi từ service)."
                                    });
                                }
                                else
                                {
                                    successCount++;
                                    _logger.LogInformation("User {Username} (Role: {Role}) successfully created asset for row {Row} with ID {AssetId}",
                                        username, role, rowNumber, createdAsset.asset_id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) encountered error creating asset for row {Row}: {Error}",
                                    username, role, rowNumber, ex.Message);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(asset),
                                    ErrorMessage = $"Lỗi khi tạo tài sản: {ex.Message}"
                                });
                            }
                        }

                        if (errorRows.Any())
                        {
                            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                            using (var errorPackage = new ExcelPackage())
                            {
                                var errorWorksheet = errorPackage.Workbook.Worksheets.Add("Error Rows");
                                errorWorksheet.Cells[1, 1].Value = "Row Number";
                                errorWorksheet.Cells[1, 2].Value = "Original Data";
                                errorWorksheet.Cells[1, 3].Value = "Error Message";

                                for (int i = 0; i < errorRows.Count; i++)
                                {
                                    errorWorksheet.Cells[i + 2, 1].Value = errorRows[i].RowNumber;
                                    errorWorksheet.Cells[i + 2, 2].Value = errorRows[i].OriginalData;
                                    errorWorksheet.Cells[i + 2, 3].Value = errorRows[i].ErrorMessage;
                                }

                                errorWorksheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;
                                errorWorksheet.Cells.AutoFitColumns();

                                var errorStream = new MemoryStream(errorPackage.GetAsByteArray());
                                TempData["SuccessCount"] = successCount;
                                TempData["ErrorFile"] = Convert.ToBase64String(errorStream.ToArray());
                                _logger.LogInformation("User {Username} (Role: {Role}) imported {SuccessCount} assets with {ErrorCount} errors",
                                    username, role, successCount, errorRows.Count);
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                            _logger.LogInformation("User {Username} (Role: {Role}) successfully imported {SuccessCount} assets with no errors",
                                username, role, successCount);
                        }

                        return RedirectToPage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}",
                    username, role, ex.Message);
                return BadRequest($"Lỗi khi xử lý file Excel: {ex.Message}");
            }
        }
    }
}