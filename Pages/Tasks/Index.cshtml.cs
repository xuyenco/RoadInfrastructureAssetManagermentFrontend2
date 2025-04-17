using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Collections.Generic;
using System.IO;
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
                var tasks = await _tasksService.GetAllTasksAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

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
                            ? $"{task.geometry.type}: {System.Text.Json.JsonSerializer.Serialize(task.geometry.coordinates)}"
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

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var result = await _tasksService.DeleteTaskAsync(id);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}