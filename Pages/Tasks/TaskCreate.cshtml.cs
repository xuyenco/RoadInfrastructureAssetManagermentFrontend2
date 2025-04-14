using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Service;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Tasks
{
    public class TaskCreateModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public TaskCreateModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        [BindProperty]
        public TasksRequest Task { get; set; } = new TasksRequest();

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }
        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for Task input");
                var header = new List<string> { "Asset Id", "Assigned To", "Task Type", "Description", "Priority ('low', 'medium', 'high')", "Status ('pending', 'in-progress', 'completed', 'cancelled')", "Due Date" };
                for (int i = 0; i < header.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = header[i];
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Task Template.xlsx");
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
                var Tasks = new List<TasksRequest>();
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
                            Console.WriteLine("File Excel is empty or invalid.");
                            return BadRequest("File Excel trống hoặc không hợp lệ.");
                        }

                        var headers = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Text?.ToLower() ?? "");
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var task = new TasksRequest();
                            var rowData = new Dictionary<string, string>();
                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "asset id":
                                        task.asset_id = int.TryParse(value, out var calId) ? calId : 0;
                                        break;
                                    case "assigned to":
                                        task.assigned_to = int.TryParse(value, out var fisyear) ? fisyear : 0;
                                        break;
                                    case "task type":
                                        task.task_type = value;
                                        break;
                                    case "description":
                                        task.description = value;
                                        break;
                                    case "priority":
                                        task.priority = value;
                                        break;
                                    case "status":
                                        task.status = value;
                                        break;
                                    case "due date":
                                        task.due_date = DateTime.TryParse(value, out var duedate) ? duedate : null;
                                        break; 
                                }
                            }
                            Tasks.Add(task);
                        }

                        int successCount = 0;
                        for (int i = 0; i < Tasks.Count; i++)
                        {
                            var task = Tasks[i];
                            var rowNumber = i + 2;

                            if (!new[] { "low", "medium", "high"}.Contains(Tasks[i].priority))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(Tasks[i]),
                                    ErrorMessage = "Dữ liệu không hợp lệ: Priority phải là 1 trong những giá trị sau: low,medium,high."
                                });
                                continue;
                            }
                            if (!new[] { "pending", "in-progress", "completed", "cancelled" }.Contains(Tasks[i].status))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(Tasks[i]),
                                    ErrorMessage = "Dữ liệu không hợp lệ: Status phải là 1 trong những giá trị sau: pending, in-progress,completed,canvelled."
                                });
                                continue;
                            }

                            try
                            {
                                var createTask = await _tasksService.CreateTaskAsync(task);
                                if (createTask == null)
                                {
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(task),
                                        ErrorMessage = "Không thể tạo task (lỗi từ service)."
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
                                    OriginalData = JsonSerializer.Serialize(task),
                                    ErrorMessage = $"Lỗi khi tạo tài sản: {ex.Message}"
                                });
                            }
                        }

                        if (errorRows.Any())
                        {
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
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                        }

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
                return Page(); // Trả về trang nếu dữ liệu không hợp lệ
            }

            try
            {
                Console.WriteLine($"Creating task with Asset ID: {Task.asset_id}, Assigned To: {Task.assigned_to}");
                var createdTask = await _tasksService.CreateTaskAsync(Task);
                if (createdTask == null)
                {
                    TempData["Error"] = "Không thể tạo Task. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Task creation failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Task đã được tạo thành công!";
                return RedirectToPage("/Tasks/Index");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Task: {ex.Message}";
                return Page();
            }
        }
    }
}