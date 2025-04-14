using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public IndexModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        public List<TasksResponse> Tasks { get; set; } = new List<TasksResponse>();

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                // Lấy dữ liệu tasks
                var tasks = await _tasksService.GetAllTasksAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Tasks Report");

                    // Thêm header
                    string[] headers = new[] {
                        "Mã Tài sản",
                        "Người được giao",
                        "Loại Nhiệm vụ",
                        "Mô tả",
                        "Ưu tiên",
                        "Trạng thái",
                        "Ngày đến hạn"
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
                        worksheet.Cells[row, 1].Value = task.asset_id;
                        worksheet.Cells[row, 2].Value = task.assigned_to;
                        worksheet.Cells[row, 3].Value = task.task_type;
                        worksheet.Cells[row, 4].Value = task.description;
                        worksheet.Cells[row, 5].Value = task.priority;
                        worksheet.Cells[row, 6].Value = task.status;
                        worksheet.Cells[row, 7].Value = task.due_date.HasValue
                            ? task.due_date.Value.ToString("dd/MM/yyyy")
                            : "Chưa có dữ liệu";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Tasks_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xuất báo cáo nhiệm vụ: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task OnGetAsync()
        {
            try
            {
                Tasks = await _tasksService.GetAllTasksAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading tasks: {ex.Message}";
                Tasks = new List<TasksResponse>();
            }
        }

        [ValidateAntiForgeryToken] // Thêm bảo mật CSRF
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Console.WriteLine($"Received task id to delete: {id}");
            try
            {
                var result = await _tasksService.DeleteTaskAsync(id);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}