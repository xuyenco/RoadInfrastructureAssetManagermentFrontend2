using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TaskCreateModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly ILogger<TaskCreateModel> _logger;

        public TaskCreateModel(ITasksService tasksService, ILogger<TaskCreateModel> logger)
        {
            _tasksService = tasksService;
            _logger = logger;
        }

        [BindProperty]
        public TasksRequest Task { get; set; } = new TasksRequest();

        [BindProperty]
        public string GeometrySystem { get; set; }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the task creation page", username, role);
            return Page();
        }

        public IActionResult OnGetDownloadExcelTemplate()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for task creation", username, role);

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for Task Input");
                var headers = new List<string>
                {
                    "Task Type",
                    "Work Volume",
                    "Status ('pending', 'in-progress', 'completed', 'cancelled')",
                    "Address",
                    "Geometry Type ('Point', 'LineString')",
                    "Geometry Coordinates (JSON format)",
                    "Geometry System ('WGS84', 'VN2000')",
                    "Start Date (yyyy-MM-dd)",
                    "End Date (yyyy-MM-dd)",
                    "Execution Unit ID",
                    "Supervisor ID",
                    "Method Summary",
                    "Main Result"
                };

                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for task creation", username, role);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Task_Template.xlsx");
            }
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is importing tasks from Excel file", username, role);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not upload an Excel file", username, role);
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToPage();
            }

            try
            {
                var tasks = new List<TasksRequest>();
                var errorRows = new List<ExcelErrorRow>();
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0;
                    ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                    using (var package = new ExcelPackage(stream))
                    {
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
                            var task = new TasksRequest { geometry = new GeoJsonGeometry() };
                            var rowData = new Dictionary<string, string>();
                            string geometrySystem = null;

                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "task type":
                                        task.task_type = value;
                                        break;
                                    case "work volume":
                                        task.work_volume = value;
                                        break;
                                    case "status ('pending', 'in-progress', 'completed', 'cancelled')":
                                        task.status = value;
                                        break;
                                    case "address":
                                        task.address = value;
                                        break;
                                    case "geometry type ('point', 'linestring')":
                                        task.geometry.type = value;
                                        break;
                                    case "geometry coordinates (json format)":
                                        task.geometry.coordinates = value;
                                        break;
                                    case "geometry system ('wgs84', 'vn2000')":
                                        geometrySystem = value;
                                        break;
                                    case "start date (yyyy-mm-dd)":
                                        task.start_date = DateTime.TryParse(value, out var startDate) ? startDate : null;
                                        break;
                                    case "end date (yyyy-mm-dd)":
                                        task.end_date = DateTime.TryParse(value, out var endDate) ? endDate : null;
                                        break;
                                    case "execution unit id":
                                        task.execution_unit_id = int.TryParse(value, out var unitId) ? unitId : null;
                                        break;
                                    case "supervisor id":
                                        task.supervisor_id = int.TryParse(value, out var supId) ? supId : null;
                                        break;
                                    case "method summary":
                                        task.method_summary = value;
                                        break;
                                    case "main result":
                                        task.main_result = value;
                                        break;
                                }
                            }

                            _logger.LogDebug("User {Username} (Role: {Role}) parsed row {Row} data: {RowData}",username, role, row, JsonSerializer.Serialize(rowData));

                            // Xử lý geometry và chuyển đổi tọa độ
                            if (!string.IsNullOrEmpty(task.geometry.type) && !string.IsNullOrEmpty(task.geometry.coordinates as string))
                            {
                                try
                                {
                                    task.geometry.coordinates = JsonSerializer.Deserialize<object>(task.geometry.coordinates as string);
                                    _logger.LogDebug("User {Username} (Role: {Role}) deserialized coordinates for row {Row}: {Coordinates}",username, role, row, JsonSerializer.Serialize(task.geometry.coordinates));

                                    // Chuyển đổi tọa độ nếu geometry_system là WGS84
                                    if (geometrySystem?.ToUpper() == "WGS84")
                                    {
                                        task.geometry = CoordinateConverter.ConvertGeometryToVN2000(task.geometry, 48);
                                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for row {Row}: {Geometry}",
                                            username, role, row, JsonSerializer.Serialize(task.geometry));
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

                            tasks.Add(task);
                        }

                        int successCount = 0;
                        for (int i = 0; i < tasks.Count; i++)
                        {
                            var task = tasks[i];
                            var rowNumber = i + 2;

                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(task.task_type) ||
                                string.IsNullOrWhiteSpace(task.status) ||
                                string.IsNullOrWhiteSpace(task.geometry.type) ||
                                task.geometry.coordinates == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid data in row {Row}: Missing task_type, status, geometry.type, or geometry.coordinates",username, role, rowNumber);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(task),
                                    ErrorMessage = "Thiếu dữ liệu bắt buộc: task_type, status, geometry.type, hoặc geometry.coordinates."
                                });
                                continue;
                            }

                            // Validate status
                            if (!new[] { "pending", "in-progress", "completed", "cancelled" }.Contains(task.status.ToLower()))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid status in row {Row}: {Status}",username, role, rowNumber, task.status);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(task),
                                    ErrorMessage = "Trạng thái không hợp lệ: phải là 'pending', 'in-progress', 'completed', hoặc 'cancelled'."
                                });
                                continue;
                            }

                            // Validate geometry type
                            if (!new[] { "Point", "LineString" }.Contains(task.geometry.type))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid geometry type in row {Row}: {GeometryType}",username, role, rowNumber, task.geometry.type);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(task),
                                    ErrorMessage = "Loại hình học không hợp lệ: phải là 'Point' hoặc 'LineString'."
                                });
                                continue;
                            }

                            try
                            {
                                _logger.LogDebug("User {Username} (Role: {Role}) creating task for row {Row}: {TaskData}",username, role, rowNumber, JsonSerializer.Serialize(task));
                                var createdTask = await _tasksService.CreateTaskAsync(task);
                                if (createdTask == null)
                                {
                                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create task for row {Row}: No result returned",username, role, rowNumber);
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(task),
                                        ErrorMessage = "Không thể tạo nhiệm vụ (lỗi từ service)."
                                    });
                                }
                                else
                                {
                                    successCount++;
                                    _logger.LogInformation("User {Username} (Role: {Role}) successfully created task ID {TaskId} for row {Row}",username, role, createdTask.task_id, rowNumber);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) encountered error creating task for row {Row}: {Error}",username, role, rowNumber, ex.Message);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(task),
                                    ErrorMessage = $"Lỗi khi tạo nhiệm vụ: {ex.Message}"
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
                                _logger.LogInformation("User {Username} (Role: {Role}) imported {SuccessCount} tasks with {ErrorCount} errors",username, role, successCount, errorRows.Count);
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                            _logger.LogInformation("User {Username} (Role: {Role}) successfully imported {SuccessCount} tasks with no errors",username, role, successCount);
                        }

                        TempData["Success"] = $"Đã nhập thành công {successCount} nhiệm vụ.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new task", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}",username, role, JsonSerializer.Serialize(Request.Form));

            if (Task.geometry == null) Task.geometry = new GeoJsonGeometry();

            var geometryType = Request.Form["Task.geometry.type"];
            if (!string.IsNullOrEmpty(geometryType) && (geometryType == "Point" || geometryType == "LineString"))
            {
                Task.geometry.type = geometryType;
                ModelState["Task.geometry.type"].Errors.Clear();
                ModelState["Task.geometry.type"].ValidationState = ModelValidationState.Valid;
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid geometry type: {GeometryType}",username, role, geometryType);
                ModelState.AddModelError("Task.geometry.type", "Loại hình học phải là 'Point' hoặc 'LineString'.");
            }

            var coordinatesJson = Request.Form["Task.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    Task.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    _logger.LogDebug("User {Username} (Role: {Role}) deserialized coordinates: {Coordinates}",username, role, JsonSerializer.Serialize(Task.geometry.coordinates));

                    if (GeometrySystem == "wgs84")
                    {
                        Task.geometry = CoordinateConverter.ConvertGeometryToVN2000(Task.geometry, 48);
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000: {Geometry}",username, role, JsonSerializer.Serialize(Task.geometry));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid coordinates: {Error}",username, role, ex.Message);
                    ModelState.AddModelError("Task.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry coordinates", username, role);
                ModelState.AddModelError("Task.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(Task.task_type))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide task_type", username, role);
                ModelState.AddModelError("Task.task_type", "Loại nhiệm vụ là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(Task.status))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide status", username, role);
                ModelState.AddModelError("Task.status", "Trạng thái là bắt buộc.");
            }
            else if (!new[] { "pending", "in-progress", "completed", "cancelled" }.Contains(Task.status.ToLower()))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid status: {Status}",username, role, Task.status);
                ModelState.AddModelError("Task.status", "Trạng thái không hợp lệ: phải là 'pending', 'in-progress', 'completed', hoặc 'cancelled'.");
            }

            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.ToDictionary(
            //        kvp => kvp.Key,
            //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            //    );
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors: {Errors}",username, role, JsonSerializer.Serialize(errors));
            //    return Page();
            //}

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) creating task: {TaskData}",username, role, JsonSerializer.Serialize(Task));
                var createdTask = await _tasksService.CreateTaskAsync(Task);
                if (createdTask == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create task: No result returned", username, role);
                    TempData["Error"] = "Không thể tạo nhiệm vụ. Dữ liệu trả về từ dịch vụ là null.";
                    return Page();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully created task ID {TaskId}",username, role, createdTask.task_id);
                TempData["Success"] = "Nhiệm vụ đã được tạo thành công!";
                return RedirectToPage("/Tasks/Index");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered JSON error: {Error}",username, role, ex.Message);
                ModelState.AddModelError("Task.geometry.coordinates", "Tọa độ phải là JSON hợp lệ.");
                return Page();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error: {Error}",username, role, ex.Message);
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error: {Error}",username, role, ex.Message);
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error creating task: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo nhiệm vụ: {ex.Message}";
                return Page();
            }
        }
    }
}