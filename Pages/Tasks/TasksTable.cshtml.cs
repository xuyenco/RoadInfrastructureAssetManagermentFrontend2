using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Drawing;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class TasksTableModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly ILogger<TasksTableModel> _logger;

        public TasksTableModel(ITasksService tasksService, ILogger<TasksTableModel> logger)
        {
            _tasksService = tasksService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the tasks table page", username, role);
            return Page();
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
                var deleted = await _tasksService.DeleteTaskAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete task with ID {TaskId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Xóa nhiệm vụ thất bại." });
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully Deleted task with ID {TaskId}", username, role, id);
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Tasks Report");

                    string[] headers = new[]
                    {
                        "Mã nhiệm vụ",
                        "Loại nhiệm vụ",
                        "Trạng thái",
                        "Địa chỉ",
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
                        worksheet.Cells[row, 2].Value = task.task_type;
                        worksheet.Cells[row, 3].Value = task.status;
                        worksheet.Cells[row, 4].Value = task.address;
                        worksheet.Cells[row, 5].Value = task.created_at.HasValue
                            ? task.created_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có";
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