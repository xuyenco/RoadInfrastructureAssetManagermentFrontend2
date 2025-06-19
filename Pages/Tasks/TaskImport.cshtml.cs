using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TaskImportModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly INotificationsService _notificationsService;
        private readonly ILogger<TaskImportModel> _logger;

        public TaskImportModel(ITasksService tasksService, INotificationsService notificationsService, ILogger<TaskImportModel> logger)
        {
            _tasksService = tasksService;
            _notificationsService = notificationsService;
            _logger = logger;
        }

        public IActionResult OnGetDownloadExcelTemplate()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for task import", username, role);

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template for Task Import");
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
                "Message for Execution Unit",
                "Supervisor ID",
                "Message for Supervisor",
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
            _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for task import", username, role);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Task_Create_Template.xlsx");
        }

        public IActionResult OnGetDownloadExcelUpdateTemplate()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for task update", username, role);

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Template for Task Update");
            var headers = new List<string>
            {
                "Id",
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
                "Message for Execution Unit",
                "Supervisor ID",
                "Message for Supervisor",
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
            _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for task update", username, role);
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Task_Update_Template.xlsx");
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
                var tasks = new List<(TasksRequest Task, NotificationsRequest SupervisorNotification, NotificationsRequest ExecutionUnitNotification)>();
                var errorRows = new List<ExcelErrorRow>();

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
                    var task = new TasksRequest { geometry = new GeoJsonGeometry() };
                    var supervisorNotification = new NotificationsRequest();
                    var executionUnitNotification = new NotificationsRequest();
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
                            case "message for execution unit":
                                executionUnitNotification.message = value;
                                break;
                            case "supervisor id":
                                task.supervisor_id = int.TryParse(value, out var supId) ? supId : null;
                                break;
                            case "message for supervisor":
                                supervisorNotification.message = value;
                                break;
                            case "method summary":
                                task.method_summary = value;
                                break;
                            case "main result":
                                task.main_result = value;
                                break;
                        }
                    }

                    _logger.LogDebug("User {Username} (Role: {Role}) parsed row {Row} data: {RowData}", username, role, row, JsonSerializer.Serialize(rowData));

                    // Xử lý geometry
                    if (!string.IsNullOrEmpty(task.geometry.type) && !string.IsNullOrEmpty(task.geometry.coordinates as string))
                    {
                        try
                        {
                            task.geometry.coordinates = JsonSerializer.Deserialize<object>(task.geometry.coordinates as string);
                            if (geometrySystem?.ToUpper() == "WGS84")
                            {
                                task.geometry = CoordinateConverter.ConvertGeometryToVN2000(task.geometry, 48);
                                _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for row {Row}: {Geometry}", username, role, row, JsonSerializer.Serialize(task.geometry));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) encountered geometry error in row {Row}: {Error}", username, role, row, ex.Message);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) missing geometry type or coordinates in row {Row}", username, role, row);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = row,
                            OriginalData = JsonSerializer.Serialize(rowData),
                            ErrorMessage = "Thiếu geometry type hoặc geometry coordinates."
                        });
                        continue;
                    }

                    tasks.Add((task, supervisorNotification, executionUnitNotification));
                }

                int successCount = 0;
                int notificationFailures = 0;
                for (int i = 0; i < tasks.Count; i++)
                {
                    var (task, supervisorNotification, executionUnitNotification) = tasks[i];
                    var rowNumber = i + 2;

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(task.task_type) ||
                        string.IsNullOrWhiteSpace(task.status) ||
                        string.IsNullOrWhiteSpace(task.geometry.type) ||
                        task.geometry.coordinates == null)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid data in row {Row}: Missing required fields", username, role, rowNumber);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(task),
                            ErrorMessage = "Thiếu dữ liệu bắt buộc: task_type, status, geometry.type, hoặc geometry.coordinates."
                        });
                        continue;
                    }

                    if (!new[] { "pending", "in-progress", "completed", "cancelled" }.Contains(task.status.ToLower()))
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid status in row {Row}: {Status}", username, role, rowNumber, task.status);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(task),
                            ErrorMessage = "Trạng thái không hợp lệ: phải là 'pending', 'in-progress', 'completed', hoặc 'cancelled'."
                        });
                        continue;
                    }

                    if (!new[] { "Point", "LineString" }.Contains(task.geometry.type))
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid geometry type in row {Row}: {GeometryType}", username, role, rowNumber, task.geometry.type);
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
                        _logger.LogDebug("User {Username} (Role: {Role}) creating task for row {Row}: {TaskData}", username, role, rowNumber, JsonSerializer.Serialize(task));
                        var createdTask = await _tasksService.CreateTaskAsync(task);
                        if (createdTask == null)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) failed to create task for row {Row}: No result returned", username, role, rowNumber);
                            errorRows.Add(new ExcelErrorRow
                            {
                                RowNumber = rowNumber,
                                OriginalData = JsonSerializer.Serialize(task),
                                ErrorMessage = "Không thể tạo nhiệm vụ (lỗi từ service)."
                            });
                            continue;
                        }

                        // Tạo thông báo
                        if (createdTask.supervisor_id.HasValue && createdTask.supervisor_id.Value > 0)
                        {
                            supervisorNotification.notification_type = "Giám sát";
                            supervisorNotification.is_read = false;
                            supervisorNotification.task_id = createdTask.task_id;
                            supervisorNotification.user_id = createdTask.supervisor_id.Value;
                            var supervisorResult = await _notificationsService.CreateNotificationAsync(supervisorNotification);
                            if (supervisorResult == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to create supervisor notification for task ID {TaskId} in row {Row}", username, role, createdTask.task_id, rowNumber);
                                notificationFailures++;
                            }
                        }

                        if (createdTask.execution_unit_id.HasValue && createdTask.execution_unit_id.Value > 0)
                        {
                            executionUnitNotification.notification_type = createdTask.task_type;
                            executionUnitNotification.is_read = false;
                            executionUnitNotification.task_id = createdTask.task_id;
                            executionUnitNotification.user_id = createdTask.execution_unit_id.Value;
                            var executionUnitResult = await _notificationsService.CreateNotificationAsync(executionUnitNotification);
                            if (executionUnitResult == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to create execution unit notification for task ID {TaskId} in row {Row}", username, role, createdTask.task_id, rowNumber);
                                notificationFailures++;
                            }
                        }

                        successCount++;
                        _logger.LogInformation("User {Username} (Role: {Role}) successfully created task ID {TaskId} for row {Row}", username, role, createdTask.task_id, rowNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) encountered error creating task for row {Row}: {Error}", username, role, rowNumber, ex.Message);
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
                    _logger.LogInformation("User {Username} (Role: {Role}) imported {SuccessCount} tasks with {ErrorCount} errors", username, role, successCount, errorRows.Count);
                }
                else
                {
                    TempData["SuccessCount"] = successCount;
                    _logger.LogInformation("User {Username} (Role: {Role}) successfully imported {SuccessCount} tasks with no errors", username, role, successCount);
                }

                if (notificationFailures > 0)
                {
                    TempData["Warning"] = $"Đã nhập thành công {successCount} nhiệm vụ, nhưng {notificationFailures} thông báo không thể gửi.";
                }
                else
                {
                    TempData["Success"] = $"Đã nhập thành công {successCount} nhiệm vụ.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportUpdateExcelAsync(IFormFile excelFile)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is updating tasks from Excel file", username, role);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not upload an Excel file", username, role);
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return RedirectToPage();
            }

            try
            {
                var tasks = new List<(int id, TasksRequest Task, NotificationsRequest SupervisorNotification, NotificationsRequest ExecutionUnitNotification)>();
                var errorRows = new List<ExcelErrorRow>();

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
                    var task = new TasksRequest { geometry = new GeoJsonGeometry() };
                    var supervisorNotification = new NotificationsRequest();
                    var executionUnitNotification = new NotificationsRequest();
                    var rowData = new Dictionary<string, string>();
                    string geometrySystem = null;
                    int? id = null;

                    for (int col = 1; col <= colCount; col++)
                    {
                        var header = headers[col - 1];
                        var value = worksheet.Cells[row, col].Text;
                        rowData[header] = value;

                        switch (header)
                        {
                            case "id":
                                id = int.TryParse(value, out var parsedId) ? parsedId : null;
                                break;
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
                            case "message for execution unit":
                                executionUnitNotification.message = value;
                                break;
                            case "supervisor id":
                                task.supervisor_id = int.TryParse(value, out var supId) ? supId : null;
                                break;
                            case "message for supervisor":
                                supervisorNotification.message = value;
                                break;
                            case "method summary":
                                task.method_summary = value;
                                break;
                            case "main result":
                                task.main_result = value;
                                break;
                        }
                    }

                    _logger.LogDebug("User {Username} (Role: {Role}) parsed row {Row} data: {RowData}", username, role, row, JsonSerializer.Serialize(rowData));

                    // Kiểm tra ID
                    if (!id.HasValue)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) missing or invalid ID in row {Row}", username, role, row);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = row,
                            OriginalData = JsonSerializer.Serialize(rowData),
                            ErrorMessage = "Thiếu hoặc ID không hợp lệ."
                        });
                        continue;
                    }

                    // Xử lý geometry
                    if (!string.IsNullOrEmpty(task.geometry.type) && !string.IsNullOrEmpty(task.geometry.coordinates as string))
                    {
                        try
                        {
                            task.geometry.coordinates = JsonSerializer.Deserialize<object>(task.geometry.coordinates as string);
                            if (geometrySystem?.ToUpper() == "WGS84")
                            {
                                task.geometry = CoordinateConverter.ConvertGeometryToVN2000(task.geometry, 48);
                                _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for row {Row}: {Geometry}", username, role, row, JsonSerializer.Serialize(task.geometry));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) encountered geometry error in row {Row}: {Error}", username, role, row, ex.Message);
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
                        _logger.LogWarning("User {Username} (Role: {Role}) missing geometry type or coordinates in row {Row}", username, role, row);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = row,
                            OriginalData = JsonSerializer.Serialize(rowData),
                            ErrorMessage = "Thiếu geometry type hoặc geometry coordinates."
                        });
                        continue;
                    }

                    tasks.Add((id.Value, task, supervisorNotification, executionUnitNotification));
                }

                int successCount = 0;
                int notificationFailures = 0;
                for (int i = 0; i < tasks.Count; i++)
                {
                    var (id, task, supervisorNotification, executionUnitNotification) = tasks[i];
                    var rowNumber = i + 2;

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(task.task_type) ||
                        string.IsNullOrWhiteSpace(task.status) ||
                        string.IsNullOrWhiteSpace(task.geometry.type) ||
                        task.geometry.coordinates == null)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid data in row {Row}: Missing required fields", username, role, rowNumber);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(task),
                            ErrorMessage = "Thiếu dữ liệu bắt buộc: task_type, status, geometry.type, hoặc geometry.coordinates."
                        });
                        continue;
                    }

                    if (!new[] { "pending", "in-progress", "completed", "cancelled" }.Contains(task.status.ToLower()))
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid status in row {Row}: {Status}", username, role, rowNumber, task.status);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(task),
                            ErrorMessage = "Trạng thái không hợp lệ: phải là 'pending', 'in-progress', 'completed', hoặc 'cancelled'."
                        });
                        continue;
                    }

                    if (!new[] { "Point", "LineString" }.Contains(task.geometry.type))
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) provided invalid geometry type in row {Row}: {GeometryType}", username, role, rowNumber, task.geometry.type);
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
                        _logger.LogDebug("User {Username} (Role: {Role}) updating task ID {Id} for row {Row}: {TaskData}", username, role, id, rowNumber, JsonSerializer.Serialize(task));
                        var updatedTask = await _tasksService.UpdateTaskAsync(id, task);
                        if (updatedTask == null)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) failed to update task ID {Id} for row {Row}: No result returned", username, role, id, rowNumber);
                            errorRows.Add(new ExcelErrorRow
                            {
                                RowNumber = rowNumber,
                                OriginalData = JsonSerializer.Serialize(task),
                                ErrorMessage = $"Không thể cập nhật nhiệm vụ ID {id} (lỗi từ service)."
                            });
                            continue;
                        }

                        // Tạo thông báo
                        if (updatedTask.supervisor_id.HasValue && updatedTask.supervisor_id.Value > 0)
                        {
                            supervisorNotification.notification_type = "Giám sát";
                            supervisorNotification.is_read = false;
                            supervisorNotification.task_id = updatedTask.task_id;
                            supervisorNotification.user_id = updatedTask.supervisor_id.Value;
                            var supervisorResult = await _notificationsService.CreateNotificationAsync(supervisorNotification);
                            if (supervisorResult == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to create supervisor notification for task ID {TaskId} in row {Row}", username, role, updatedTask.task_id, rowNumber);
                                notificationFailures++;
                            }
                        }

                        if (updatedTask.execution_unit_id.HasValue && updatedTask.execution_unit_id.Value > 0)
                        {
                            executionUnitNotification.notification_type = updatedTask.task_type;
                            executionUnitNotification.is_read = false;
                            executionUnitNotification.task_id = updatedTask.task_id;
                            executionUnitNotification.user_id = updatedTask.execution_unit_id.Value;
                            var executionUnitResult = await _notificationsService.CreateNotificationAsync(executionUnitNotification);
                            if (executionUnitResult == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to create execution unit notification for task ID {TaskId} in row {Row}", username, role, updatedTask.task_id, rowNumber);
                                notificationFailures++;
                            }
                        }

                        successCount++;
                        _logger.LogInformation("User {Username} (Role: {Role}) successfully updated task ID {TaskId} for row {Row}", username, role, updatedTask.task_id, rowNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) encountered error updating task ID {Id} for row {Row}: {Error}", username, role, id, rowNumber, ex.Message);
                        errorRows.Add(new ExcelErrorRow
                        {
                            RowNumber = rowNumber,
                            OriginalData = JsonSerializer.Serialize(task),
                            ErrorMessage = $"Lỗi khi cập nhật nhiệm vụ ID {id}: {ex.Message}"
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
                    _logger.LogInformation("User {Username} (Role: {Role}) updated {SuccessCount} tasks with {ErrorCount} errors", username, role, successCount, errorRows.Count);
                }
                else
                {
                    TempData["SuccessCount"] = successCount;
                    _logger.LogInformation("User {Username} (Role: {Role}) successfully updated {SuccessCount} tasks with no errors", username, role, successCount);
                }

                if (notificationFailures > 0)
                {
                    TempData["Warning"] = $"Đã cập nhật thành công {successCount} nhiệm vụ, nhưng {notificationFailures} thông báo không thể gửi.";
                }
                else
                {
                    TempData["Success"] = $"Đã cập nhật thành công {successCount} nhiệm vụ.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}