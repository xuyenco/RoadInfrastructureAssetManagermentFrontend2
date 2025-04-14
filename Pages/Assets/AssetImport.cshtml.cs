using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Assets
{
    public class AssetImportModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public AssetImportModel(IAssetsService assetsService, IAssetCagetoriesService assetCagetoriesService)
        {
            _assetsService = assetsService;
            _assetCagetoriesService = assetCagetoriesService;
        }

        public List<AssetCagetoriesResponse> Categories { get; set; }
        public async Task OnGetAsync()
        {
            Categories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync();
        }
        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync(int categoryId)
        {
            if (categoryId == 0) {
                return BadRequest("Vui lòng chọn danh mục");
            }
            var category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(categoryId);
            if (category == null)
            {
                return NotFound("Category không tồn tại");
            }

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"Asset template for {category.cagetory_name}");
                var fixedColumns = new List<string>
                {
                    "cagetory_id", "condition", "lifecycle_stage", "installation_date", "expected_lifetime", "last_inspection_date", "geometry_type", "geometry_coordinates"
                };
                // Seprate atributes_schema to dictionary
                var properties = category.attributes_schema != null &&
                                category.attributes_schema.TryGetValue("properties", out var props) &&
                                props != null
                                ? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(props)) : new Dictionary<string, object>();
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

                // Định dạng cột
                worksheet.Cells[1, 1, 1, fixedColumns.Count + attributeColumns.Count].Style.Font.Bold = true;
                worksheet.Cells.AutoFitColumns();

                // Trả về file Excel
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Asset_Template_Category_{categoryId}.xlsx");
            }
        }
        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                Console.WriteLine("No file uploaded.");
                return BadRequest("Vui lòng chọn file Excel.");
            }

            try
            {
                // Đọc file Excel
                var assets = new List<AssetsRequest>();
                var errorRows = new List<ExcelErrorRow>(); // Lưu các dòng lỗi
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
                            Console.WriteLine("File Excel is empty or invalid.");
                            return BadRequest("File Excel trống hoặc không hợp lệ.");
                        }

                        var headers = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Text?.ToLower() ?? "");
                        }

                        // Đọc dữ liệu
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var asset = new AssetsRequest { attributes = new Dictionary<string, object>() };
                            var rowData = new Dictionary<string, string>(); // Lưu dữ liệu gốc của dòng

                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value; // Lưu dữ liệu gốc

                                switch (header)
                                {
                                    case "cagetory_id":
                                        asset.cagetory_id = int.TryParse(value, out var catId) ? catId : 0; break;
                                    case "condition":
                                        asset.condition = value; break;
                                    case "lifecycle_stage":
                                        asset.lifecycle_stage = value; break;
                                    case "installation_date":
                                        asset.installation_date = DateTime.TryParse(value, out var installDate) ? installDate : null; break;
                                    case "expected_lifetime":
                                        asset.expected_lifetime = int.TryParse(value, out var lifetime) ? lifetime : 0; break;
                                    case "last_inspection_date":
                                        asset.last_inspection_date = DateTime.TryParse(value, out var inspectionDate) ? inspectionDate : null; break;
                                    case "geometry_type":
                                        asset.geometry = new GeoJsonGeometry { type = value }; break;
                                    case "geometry_coordinates":
                                        asset.geometry.coordinates = string.IsNullOrEmpty(value) ? null : JsonSerializer.Deserialize<object>(value); break;
                                    default:
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            if (int.TryParse(value, out var intValue))
                                                asset.attributes[header] = intValue;
                                            else
                                                asset.attributes[header] = value;
                                        }
                                        break;
                                }
                            }

                            assets.Add(asset);
                        }

                        // Xử lý tuần tự và ghi nhận lỗi
                        int successCount = 0;
                        for (int i = 0; i < assets.Count; i++)
                        {
                            var asset = assets[i];
                            var rowNumber = i + 2; // Dòng 1 là header, dữ liệu bắt đầu từ dòng 2

                            // Validate cơ bản trước khi gửi
                            if (asset.cagetory_id == 0 || string.IsNullOrEmpty(asset.condition) || string.IsNullOrEmpty(asset.lifecycle_stage))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(assets[i]),
                                    ErrorMessage = "Dữ liệu không hợp lệ: Thiếu cagetory_id, condition hoặc lifecycle_stage."
                                });
                                continue;
                            }

                            try
                            {
                                var createdAsset = await _assetsService.CreateAssetAsync(asset);
                                if (createdAsset == null)
                                {
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
                                }
                            }
                            catch (Exception ex)
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(asset),
                                    ErrorMessage = $"Lỗi khi tạo tài sản: {ex.Message}"
                                });
                            }
                        }

                        // Tạo file Excel chứa lỗi nếu có
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
                                TempData["ErrorFile"] = Convert.ToBase64String(errorStream.ToArray()); // Lưu tạm file lỗi vào TempData
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                        }

                        // Chuyển hướng về trang hiện tại để hiển thị kết quả
                        return RedirectToPage();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Excel file: {ex.Message}");
                return BadRequest($"Lỗi khi xử lý file Excel: {ex.Message}");
            }
        }
    }
}
