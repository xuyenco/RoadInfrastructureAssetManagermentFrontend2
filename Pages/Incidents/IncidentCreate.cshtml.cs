using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentCreateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public IncidentCreateModel(
            IIncidentsService incidentsService,
            IIncidentImageService incidentImageService,
            IAssetCagetoriesService assetCagetoriesService)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public IncidentsRequest Incident { get; set; } = new IncidentsRequest();

        [BindProperty]
        public IFormFile[] Images { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            return Page();
        }

        public IActionResult OnGetDownloadExcelTemplate()
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template for Incident Input");
            var headers = new List<string>
            {
                "Address",
                "Route",
                "Geometry Type ('Point', 'LineString')",
                "Geometry Coordinates (JSON format)",
                "Geometry System ('WGS84', 'VN2000')",
                "Severity Level ('low', 'medium', 'high', 'critical')",
                "Damage Level ('minor', 'moderate', 'severe')",
                "Processing Status ('reported', 'under review', 'resolved', 'closed')",
                "Task ID",
                "Image Paths (comma-separated)"
            };

            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            worksheet.Cells.AutoFitColumns();
            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Incident_Template.xlsx");
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile, IFormFileCollection imageFiles)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                Console.WriteLine("No Excel file uploaded.");
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToPage();
            }

            try
            {
                var incidents = new List<(IncidentsRequest incident, string[] imagePaths)>();
                var errorRows = new List<ExcelErrorRow>();
                var imageFileMap = imageFiles.ToDictionary(f => f.FileName, f => f, StringComparer.OrdinalIgnoreCase);

                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);
                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount < 2 || colCount < 1)
                {
                    Console.WriteLine("Excel file is empty or invalid.");
                    TempData["Error"] = "File Excel trống hoặc không hợp lệ.";
                    return RedirectToPage();
                }

                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    headers.Add(worksheet.Cells[1, col].Text?.ToLower() ?? "");
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    var incident = new IncidentsRequest { geometry = new GeoJsonGeometry() };
                    var rowData = new Dictionary<string, string>();
                    string geometryType = null;
                    string geometryCoordinates = null;
                    string geometrySystem = null;
                    string imagePaths = null;

                    for (int col = 1; col <= colCount; col++)
                    {
                        var header = headers[col - 1];
                        var value = worksheet.Cells[row, col].Text;
                        rowData[header] = value;

                        switch (header)
                        {
                            case "address":
                                incident.address = value;
                                break;
                            case "route":
                                incident.route = value;
                                break;
                            case "geometry type ('point', 'linestring')":
                                geometryType = value;
                                break;
                            case "geometry coordinates (json format)":
                                geometryCoordinates = value;
                                break;
                            case "geometry system ('wgs84', 'vn2000')":
                                geometrySystem = value;
                                break;
                            case "severity level ('low', 'medium', 'high', 'critical')":
                                incident.severity_level = value;
                                break;
                            case "damage level ('minor', 'moderate', 'severe')":
                                incident.damage_level = value;
                                break;
                            case "processing status ('reported', 'under review', 'resolved', 'closed')":
                                incident.processing_status = value;
                                break;
                            case "task id":
                                incident.task_id = int.TryParse(value, out var taskId) ? taskId : null;
                                break;
                            case "image paths (comma-separated)":
                                imagePaths = value;
                                break;
                        }
                    }

                    // Log dữ liệu gốc để debug
                    Console.WriteLine($"Row {row} data: {JsonSerializer.Serialize(rowData)}");

                    // Xử lý geometry
                    if (!string.IsNullOrEmpty(geometryType) && !string.IsNullOrEmpty(geometryCoordinates))
                    {
                        try
                        {
                            incident.geometry.type = geometryType;
                            incident.geometry.coordinates = JsonSerializer.Deserialize<object>(geometryCoordinates);

                            if (geometrySystem?.ToUpper() == "WGS84")
                            {
                                incident.geometry = CoordinateConverter.ConvertGeometryToVN2000(incident.geometry, 48);
                            }
                        }
                        catch (Exception ex)
                        {
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
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = row,
                            OriginalData = JsonSerializer.Serialize(rowData),
                            ErrorMessage = "Thiếu geometry type hoặc geometry coordinates."
                        });
                        continue;
                    }

                    // Kiểm tra ảnh
                    var imagePathList = string.IsNullOrEmpty(imagePaths) ? new string[0] : imagePaths.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (var imagePath in imagePathList)
                    {
                        if (!imageFileMap.ContainsKey(Path.GetFileName(imagePath)))
                        {
                            errorRows.Add(new ExcelErrorRow
                            {
                                RowNumber = row,
                                OriginalData = JsonSerializer.Serialize(rowData),
                                ErrorMessage = $"Không tìm thấy ảnh '{imagePath}' trong số ảnh tải lên."
                            });
                            continue;
                        }
                    }

                    incidents.Add((incident, imagePathList));
                }

                int successCount = 0;
                for (int i = 0; i < incidents.Count; i++)
                {
                    var (incident, imagePathList) = incidents[i];
                    var rowNumber = i + 2;

                    // Validate required fields
                    if (string.IsNullOrEmpty(incident.severity_level) ||
                        string.IsNullOrEmpty(incident.damage_level) ||
                        string.IsNullOrEmpty(incident.processing_status))
                    {
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(incident),
                            ErrorMessage = "Thiếu dữ liệu bắt buộc: severity_level, damage_level, hoặc processing_status."
                        });
                        continue;
                    }

                    // Validate enum values
                    if (!new[] { "low", "medium", "high", "critical" }.Contains(incident.severity_level.ToLower()))
                    {
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(incident),
                            ErrorMessage = "Mức độ nghiêm trọng không hợp lệ: phải là 'low', 'medium', 'high', hoặc 'critical'."
                        });
                        continue;
                    }

                    if (!new[] { "minor", "moderate", "severe" }.Contains(incident.damage_level.ToLower()))
                    {
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(incident),
                            ErrorMessage = "Mức độ hư hỏng không hợp lệ: phải là 'minor', 'moderate', hoặc 'severe'."
                        });
                        continue;
                    }

                    if (!new[] { "reported", "under review", "resolved", "closed" }.Contains(incident.processing_status.ToLower()))
                    {
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(incident),
                            ErrorMessage = "Trạng thái xử lý không hợp lệ: phải là 'reported', 'under review', 'resolved', hoặc 'closed'."
                        });
                        continue;
                    }

                    try
                    {
                        var createdIncident = await _incidentsService.CreateIncidentAsync(incident);
                        if (createdIncident == null)
                        {
                            errorRows.Add(new ExcelErrorRow
                            {
                                RowNumber = rowNumber,
                                OriginalData = JsonSerializer.Serialize(incident),
                                ErrorMessage = "Không thể tạo incident (lỗi từ service)."
                            });
                            continue;
                        }

                        // Lưu ảnh
                        foreach (var imagePath in imagePathList)
                        {
                            if (imageFileMap.TryGetValue(Path.GetFileName(imagePath), out var imageFile))
                            {
                                var imageRequest = new IncidentImageRequest
                                {
                                    incident_id = createdIncident.incident_id,
                                    image = imageFile
                                };
                                var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                                if (createdImage == null)
                                {
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(incident),
                                        ErrorMessage = $"Không thể lưu ảnh '{imagePath}' cho incident ID {createdIncident.incident_id}."
                                    });
                                }
                            }
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(incident),
                            ErrorMessage = $"Lỗi khi tạo incident: {ex.Message}"
                        });
                    }
                }

                if (errorRows.Any())
                {
                    ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                    using var errorPackage = new ExcelPackage();
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
                }
                else
                {
                    TempData["SuccessCount"] = successCount;
                }

                TempData["Success"] = $"Đã nhập thành công {successCount} incident.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Excel file: {ex.Message}");
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var geometryType = Request.Form["Incident.geometry.type"].ToString();
            var coordinatesJson = Request.Form["Incident.geometry.coordinates"].ToString();
            var geometrySystem = Request.Form["GeometrySystem"].ToString();

            if (string.IsNullOrEmpty(geometryType))
            {
                ModelState.AddModelError("Incident.geometry.type", "Loại hình học là bắt buộc.");
            }
            else
            {
                Incident.geometry.type = geometryType;
            }

            if (string.IsNullOrEmpty(coordinatesJson))
            {
                ModelState.AddModelError("Incident.geometry.coordinates", "Tọa độ là bắt buộc.");
            }
            else
            {
                try
                {
                    Incident.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Deserialized coordinates: {JsonSerializer.Serialize(Incident.geometry.coordinates)}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                    ModelState.AddModelError("Incident.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(geometrySystem))
            {
                ModelState.AddModelError("GeometrySystem", "Hệ tọa độ là bắt buộc.");
            }
            else
            {
                GeometrySystem = geometrySystem;
                if (geometrySystem == "wgs84")
                {
                    var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.geometry, 48);
                    Incident.geometry = vn2000Geometry;
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
                AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                return Page();
            }

            try
            {
                Console.WriteLine($"Creating incident: {JsonSerializer.Serialize(Incident)}");
                var createdIncident = await _incidentsService.CreateIncidentAsync(Incident);
                if (createdIncident == null)
                {
                    TempData["Error"] = "Không thể tạo Incident. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Incident creation failed: null response from service.");
                    AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                    return Page();
                }

                if (Images != null && Images.Length > 0)
                {
                    foreach (var image in Images)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imageRequest = new IncidentImageRequest
                            {
                                incident_id = createdIncident.incident_id,
                                image = image
                            };
                            var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                            if (createdImage == null)
                            {
                                Console.WriteLine($"Failed to create image for incident ID {createdIncident.incident_id}");
                                TempData["Warning"] = "Incident đã được tạo, nhưng một số ảnh không thể thêm.";
                            }
                        }
                    }
                }

                TempData["Success"] = "Incident và ảnh (nếu có) đã được tạo thành công!";
                return RedirectToPage("/Incidents/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Incident: {ex.Message}";
                AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                return Page();
            }
        }
    }
}