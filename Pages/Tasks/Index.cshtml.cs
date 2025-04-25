using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ITasksService tasksService, ILogger<IndexModel> logger)
        {
            _tasksService = tasksService;
            _logger = logger;
        }

        public List<TasksResponse> Tasks { get; set; } = new List<TasksResponse>();

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

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Tasks Report");

                    // Thêm header
                    string[] headers = new[]
                    {
                        "Mã Nhiệm vụ",
                        "Loại Nhiệm vụ",
                        "Khối lượng công việc",
                        "Trạng thái",
                        "Địa chỉ",
                        "Hình học",
                        "Ngày bắt đầu",
                        "Ngày kết thúc",
                        "Đơn vị thực hiện",
                        "Người giám sát",
                        "Tóm tắt phương pháp",
                        "Kết quả chính"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Thêm dữ liệu
                    int row = 2;
                    foreach (var task in tasks)
                    {
                        worksheet.Cells[row, 1].Value = task.task_id;
                        worksheet.Cells[row, 2].Value = task.task_type;
                        worksheet.Cells[row, 3].Value = task.work_volume;
                        worksheet.Cells[row, 4].Value = task.status;
                        worksheet.Cells[row, 5].Value = task.address;
                        worksheet.Cells[row, 6].Value = task.geometry != null
                            ? $"{task.geometry.type}: {JsonSerializer.Serialize(task.geometry.coordinates)}"
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 7].Value = task.start_date.HasValue
                            ? task.start_date.Value.ToString("dd/MM/yyyy")
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 8].Value = task.end_date.HasValue
                            ? task.end_date.Value.ToString("dd/MM/yyyy")
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 9].Value = task.execution_unit_id.HasValue
                            ? task.execution_unit_id.Value.ToString()
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 10].Value = task.supervisor_id.HasValue
                            ? task.supervisor_id.Value.ToString()
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 11].Value = task.method_summary;
                        worksheet.Cells[row, 12].Value = task.main_result;
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Tasks_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {TaskCount} tasks to Excel file {FileName}",username, role, tasks.Count, fileName);
                    return File(stream,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting tasks to Excel: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo nhiệm vụ: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the tasks index page", username, role);

            try
            {
                Tasks = await _tasksService.GetAllTasksAsync();
                if (Tasks == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching tasks", username, role);
                    Tasks = new List<TasksResponse>();
                    TempData["Error"] = "Không thể tải danh sách nhiệm vụ.";
                }
                else
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) retrieved {TaskCount} tasks", username, role, Tasks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading tasks: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi tải danh sách nhiệm vụ: {ex.Message}";
                Tasks = new List<TasksResponse>();
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
                var result = await _tasksService.DeleteTaskAsync(id);
                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted task with ID {TaskId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error deleting task with ID {TaskId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting task with ID {TaskId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting task with ID {TaskId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }
    }
}