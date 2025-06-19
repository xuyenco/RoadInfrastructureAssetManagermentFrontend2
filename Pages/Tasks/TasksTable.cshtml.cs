using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;
using System.Drawing;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TasksTableModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly INotificationsService _notificationsService;
        private readonly IReportService _reportService;
        private readonly ILogger<TasksTableModel> _logger;

        public TasksTableModel(ITasksService tasksService, INotificationsService notificationsService, IReportService reportService, ILogger<TasksTableModel> logger)
        {
            _tasksService = tasksService;
            _notificationsService = notificationsService;
            _reportService = reportService;
            _logger = logger;
        }

        public List<TaskPerformanceReport> TaskPerformanceReport { get; set; } = new List<TaskPerformanceReport>();

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the tasks table page", username, role);

            try
            {
                TaskPerformanceReport = await _reportService.GetTaskStatusDistributions();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading task performance report: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi tải dữ liệu báo cáo: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetTasksAsync(int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is fetching tasks - Page: {CurrentPage}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, currentPage, pageSize, searchTerm, searchField);

            try
            {
                var (tasks, totalCount) = await _tasksService.GetTasksAsync(currentPage, pageSize, searchTerm, searchField);
                if (tasks == null || !tasks.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received empty tasks list for Page: {CurrentPage}", username, role, currentPage);
                    return new JsonResult(new { success = true, tasks = new List<TasksResponse>(), totalCount = 0 });
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {TaskCount} tasks with total count {TotalCount} for Page: {CurrentPage}",
                    username, role, tasks.Count, totalCount, currentPage);
                return new JsonResult(new { success = true, tasks, totalCount });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access while fetching tasks", username, role);
                HttpContext.Session.Clear();
                return new JsonResult(new { success = false, message = "Phiên làm việc hết hạn, vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error fetching tasks: {Error}", username, role, ex.Message);
                return new JsonResult(new { success = false, message = $"Lỗi khi tải danh sách nhiệm vụ: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete task with ID {TaskId}", username, role, id);

            try
            {
                var task = await _tasksService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId} for deletion", username, role, id);
                    return new JsonResult(new { success = false, message = "Không tìm thấy nhiệm vụ với ID này." });
                }

                var deleted = await _tasksService.DeleteTaskAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete task with ID {TaskId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Xóa nhiệm vụ thất bại." });
                }

                if (task.execution_unit_id.HasValue && task.execution_unit_id > 0)
                {
                    var notificationRequest = new NotificationsRequest
                    {
                        user_id = task.execution_unit_id.Value,
                        task_id = null,
                        message = $"Task {id} đã bị xóa.",
                        is_read = false,
                        notification_type = "TaskDeleted"
                    };
                    _logger.LogDebug("User {Username} (Role: {Role}) creating notification for execution unit {UserId} for deleted task ID {TaskId}: {NotificationData}",
                        username, role, task.execution_unit_id.Value, id, System.Text.Json.JsonSerializer.Serialize(notificationRequest));
                    var notificationResult = await _notificationsService.CreateNotificationAsync(notificationRequest);
                    if (notificationResult == null)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to create notification for execution unit {UserId} for deleted task ID {TaskId}",
                            username, role, task.execution_unit_id.Value, id);
                    }
                }

                if (task.supervisor_id.HasValue && task.supervisor_id > 0)
                {
                    var notificationRequest = new NotificationsRequest
                    {
                        user_id = task.supervisor_id.Value,
                        task_id = null,
                        message = $"Task {id} đã bị xóa.",
                        is_read = false,
                        notification_type = "TaskDeleted"
                    };
                    _logger.LogDebug("User {Username} (Role: {Role}) creating notification for supervisor {UserId} for deleted task ID {TaskId}: {NotificationData}",
                        username, role, task.supervisor_id.Value, id, System.Text.Json.JsonSerializer.Serialize(notificationRequest));
                    var notificationResult = await _notificationsService.CreateNotificationAsync(notificationRequest);
                    if (notificationResult == null)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to create notification for supervisor {UserId} for deleted task ID {TaskId}",
                            username, role, task.supervisor_id.Value, id);
                    }
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted task with ID {TaskId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting task with ID {TaskId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting task with ID {TaskId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi khi xóa nhiệm vụ: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is exporting tasks to Excel", username, role);

            try
            {
                var tasks = await _tasksService.GetAllTasksAsync();
                if (tasks == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching tasks for export", username, role);
                    TempData["Error"] = "Không thể tải danh sách nhiệm vụ để xuất Excel.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {TaskCount} tasks for export", username, role, tasks.Count);

                ExcelPackage.License.SetNonCommercialPersonal("Duong");
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Tasks Report");
                    string[] headers = new[]
                    {
                        "Mã nhiệm vụ",
                        "Loại nhiệm vụ",
                        "Khối lượng công việc",
                        "Trạng thái",
                        "Địa chỉ",
                        "Hình học",
                        "Ngày bắt đầu",
                        "Ngày kết thúc",
                        "ID đơn vị thực hiện",
                        "ID giám sát",
                        "Tóm tắt phương pháp",
                        "Kết quả chính",
                        "Mô tả",
                        "Ngày tạo"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    int row = 2;
                    foreach (var task in tasks)
                    {
                        worksheet.Cells[row, 1].Value = task.task_id;
                        worksheet.Cells[row, 2].Value = task.task_type ?? "Chưa xác định";
                        worksheet.Cells[row, 3].Value = task.work_volume ?? "Không có";
                        worksheet.Cells[row, 4].Value = task.status ?? "Chưa xác định";
                        worksheet.Cells[row, 5].Value = task.address ?? "Không có";
                        worksheet.Cells[row, 6].Value = task.geometry != null ? JsonSerializer.Serialize(task.geometry) : "Không có dữ liệu";
                        worksheet.Cells[row, 7].Value = task.start_date.HasValue ? task.start_date.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa có";
                        worksheet.Cells[row, 8].Value = task.end_date.HasValue ? task.end_date.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa có";
                        worksheet.Cells[row, 9].Value = task.execution_unit_id.HasValue ? task.execution_unit_id.Value.ToString() : "Không có";
                        worksheet.Cells[row, 10].Value = task.supervisor_id.HasValue ? task.supervisor_id.Value.ToString() : "Không có";
                        worksheet.Cells[row, 11].Value = task.method_summary ?? "Không có";
                        worksheet.Cells[row, 12].Value = task.main_result ?? "Không có";
                        worksheet.Cells[row, 13].Value = task.description ?? "Không có";
                        worksheet.Cells[row, 14].Value = task.created_at.HasValue ? task.created_at.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa có";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Tasks_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {TaskCount} tasks to Excel file {FileName}", username, role, tasks.Count, fileName);
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting tasks to Excel: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo nhiệm vụ: {ex.Message}";
                return RedirectToPage();
            }
        }

    }
}