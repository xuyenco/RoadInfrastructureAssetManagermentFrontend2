using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentCreateModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly IIncidentImageService _incidentImageService;
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<IncidentCreateModel> _logger;

        public IncidentCreateModel(
            IIncidentsService incidentsService,
            IIncidentImageService incidentImageService,
            IAssetCagetoriesService assetCagetoriesService,
            ILogger<IncidentCreateModel> logger)
        {
            _incidentsService = incidentsService;
            _incidentImageService = incidentImageService;
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        [BindProperty]
        public IncidentsRequest Incident { get; set; } = new IncidentsRequest { geometry = new GeoJsonGeometry() };

        [BindProperty]
        public IFormFile[] Images { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the incident creation page", username, role);
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            _logger.LogInformation("User {Username} (Role: {Role}) retrieved {CategoryCount} asset categories for incident creation",username, role, AssetCategories.Count);
            return Page();
        }

        public IActionResult OnGetDownloadExcelTemplate()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for incident creation", username, role);

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
            _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for incident creation",username, role);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Incident_Template.xlsx");
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile, IFormFileCollection imageFiles)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is importing incidents from Excel file", username, role);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not upload an Excel file", username, role);
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
                    _logger.LogWarning("User {Username} (Role: {Role}) uploaded an empty or invalid Excel file", username, role);
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

                    _logger.LogDebug("User {Username} (Role: {Role}) parsed row {Row} data: {RowData}",username, role, row, JsonSerializer.Serialize(rowData));

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
                                _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for row {Row}: {Geometry}",username, role, row, JsonSerializer.Serialize(incident.geometry));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) encountered geometry error in row {Row}: {Error}",username, role, row, ex.Message);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) missing geometry type or coordinates in row {Row}",username, role, row);
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
                            _logger.LogWarning("User {Username} (Role: {Role}) did not find image '{ImagePath}' for row {Row}",username, role, imagePath, row);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid data in row {Row}: Missing severity_level, damage_level, or processing_status",username, role, rowNumber);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid severity_level in row {Row}: {SeverityLevel}",username, role, rowNumber, incident.severity_level);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid damage_level in row {Row}: {DamageLevel}",username, role, rowNumber, incident.damage_level);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid processing_status in row {Row}: {ProcessingStatus}",username, role, rowNumber, incident.processing_status);
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
                        _logger.LogDebug("User {Username} (Role: {Role}) creating incident for row {Row}: {IncidentData}",username, role, rowNumber, JsonSerializer.Serialize(incident));
                        var createdIncident = await _incidentsService.CreateIncidentAsync(incident);
                        if (createdIncident == null)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) failed to create incident for row {Row}: No result returned",
                                username, role, rowNumber);
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
                                _logger.LogDebug("User {Username} (Role: {Role}) uploading image for incident ID {IncidentId} in row {Row}: filename={FileName}, size={Size}",
                                    username, role, createdIncident.incident_id, rowNumber, imageFile.FileName, imageFile.Length);
                                var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                                if (createdImage == null)
                                {
                                    _logger.LogWarning("User {Username} (Role: {Role}) failed to upload image for incident ID {IncidentId} in row {Row}: filename={FileName}",
                                        username, role, createdIncident.incident_id, rowNumber, imageFile.FileName);
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
                        _logger.LogInformation("User {Username} (Role: {Role}) successfully created incident ID {IncidentId} for row {Row} with {ImageCount} images",username, role, createdIncident.incident_id, rowNumber, imagePathList.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) encountered error creating incident for row {Row}: {Error}",
                            username, role, rowNumber, ex.Message);
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
                    _logger.LogInformation("User {Username} (Role: {Role}) imported {SuccessCount} incidents with {ErrorCount} errors",username, role, successCount, errorRows.Count);
                }
                else
                {
                    TempData["SuccessCount"] = successCount;
                    _logger.LogInformation("User {Username} (Role: {Role}) successfully imported {SuccessCount} incidents with no errors",username, role, successCount);
                }

                TempData["Success"] = $"Đã nhập thành công {successCount} incident.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new incident", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}",username, role, JsonSerializer.Serialize(Request.Form));

            var geometryType = Request.Form["Incident.geometry.type"].ToString();
            var coordinatesJson = Request.Form["Incident.geometry.coordinates"].ToString();
            var geometrySystem = Request.Form["GeometrySystem"].ToString();

            if (string.IsNullOrEmpty(geometryType))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry type", username, role);
                ModelState.AddModelError("Incident.geometry.type", "Loại hình học là bắt buộc.");
            }
            else
            {
                Incident.geometry.type = geometryType;
            }

            if (string.IsNullOrEmpty(coordinatesJson))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry coordinates", username, role);
                ModelState.AddModelError("Incident.geometry.coordinates", "Tọa độ là bắt buộc.");
            }
            else
            {
                try
                {
                    Incident.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    _logger.LogDebug("User {Username} (Role: {Role}) deserialized coordinates: {Coordinates}",username, role, JsonSerializer.Serialize(Incident.geometry.coordinates));
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid coordinates: {Error}",username, role, ex.Message);
                    ModelState.AddModelError("Incident.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(geometrySystem))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry system", username, role);
                ModelState.AddModelError("GeometrySystem", "Hệ tọa độ là bắt buộc.");
            }
            else
            {
                GeometrySystem = geometrySystem;
                if (geometrySystem == "wgs84")
                {
                    try
                    {
                        var vn2000Geometry = CoordinateConverter.ConvertGeometryToVN2000(Incident.geometry, 48);
                        Incident.geometry = vn2000Geometry;
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000: {Geometry}",username, role, JsonSerializer.Serialize(Incident.geometry));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to convert geometry to VN2000: {Error}",username, role, ex.Message);
                        ModelState.AddModelError("Incident.geometry", $"Lỗi chuyển đổi tọa độ: {ex.Message}");
                    }
                }
            }

            // Validate required fields and enum values
            if (string.IsNullOrEmpty(Incident.severity_level))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide severity_level", username, role);
                ModelState.AddModelError("Incident.severity_level", "Mức độ nghiêm trọng là bắt buộc.");
            }
            else if (!new[] { "low", "medium", "high", "critical" }.Contains(Incident.severity_level.ToLower()))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid severity_level: {SeverityLevel}",username, role, Incident.severity_level);
                ModelState.AddModelError("Incident.severity_level", "Mức độ nghiêm trọng không hợp lệ: phải là 'low', 'medium', 'high', hoặc 'critical'.");
            }

            if (string.IsNullOrEmpty(Incident.damage_level))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide damage_level", username, role);
                ModelState.AddModelError("Incident.damage_level", "Mức độ hư hỏng là bắt buộc.");
            }
            else if (!new[] { "minor", "moderate", "severe" }.Contains(Incident.damage_level.ToLower()))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid damage_level: {DamageLevel}",username, role, Incident.damage_level);
                ModelState.AddModelError("Incident.damage_level", "Mức độ hư hỏng không hợp lệ: phải là 'minor', 'moderate', hoặc 'severe'.");
            }

            if (string.IsNullOrEmpty(Incident.processing_status))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide processing_status", username, role);
                ModelState.AddModelError("Incident.processing_status", "Trạng thái xử lý là bắt buộc.");
            }
            else if (!new[] { "reported", "under review", "resolved", "closed" }.Contains(Incident.processing_status.ToLower()))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid processing_status: {ProcessingStatus}",username, role, Incident.processing_status);
                ModelState.AddModelError("Incident.processing_status", "Trạng thái xử lý không hợp lệ: phải là 'reported', 'under review', 'resolved', hoặc 'closed'.");
            }

            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.ToDictionary(
            //        kvp => kvp.Key,
            //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            //    );
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors: {Errors}",username, role, JsonSerializer.Serialize(errors));
            //    AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
            //    return Page();
            //}

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) creating incident: {IncidentData}",username, role, JsonSerializer.Serialize(Incident));
                var createdIncident = await _incidentsService.CreateIncidentAsync(Incident);
                if (createdIncident == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create incident: No result returned",username, role);
                    TempData["Error"] = "Không thể tạo Incident. Dữ liệu trả về từ dịch vụ là null.";
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
                            _logger.LogDebug("User {Username} (Role: {Role}) uploading image for incident ID {IncidentId}: filename={FileName}, size={Size}",username, role, createdIncident.incident_id, image.FileName, image.Length);
                            var createdImage = await _incidentImageService.CreateIncidentImageAsync(imageRequest);
                            if (createdImage == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to upload image for incident ID {IncidentId}: filename={FileName}",username, role, createdIncident.incident_id, image.FileName);
                                TempData["Warning"] = "Incident đã được tạo, nhưng một số ảnh không thể thêm.";
                            }
                        }
                    }
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully created incident ID {IncidentId} with {ImageCount} images",username, role, createdIncident.incident_id, Images?.Length ?? 0);
                TempData["Success"] = "Incident và ảnh (nếu có) đã được tạo thành công!";
                return RedirectToPage("/Incidents/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error creating incident: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Incident: {ex.Message}";
                AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
                return Page();
            }
        }
    }
}