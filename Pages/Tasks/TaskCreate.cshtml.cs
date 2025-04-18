using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;



namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
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

        [BindProperty]
        public string GeometrySystem { get; set; }

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }

        public IActionResult OnGetDownloadExcelTemplate()
        {
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
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Task_Template.xlsx");
            }
        }

        public async Task<IActionResult> OnPostImportExcelAsync(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
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

                            // Xử lý geometry và chuyển đổi tọa độ
                            if (!string.IsNullOrEmpty(task.geometry.type) && !string.IsNullOrEmpty(task.geometry.coordinates as string))
                            {
                                try
                                {
                                    task.geometry.coordinates = JsonSerializer.Deserialize<object>(task.geometry.coordinates as string);

                                    // Chuyển đổi tọa độ nếu geometry_system là WGS84
                                    if (geometrySystem?.ToUpper() == "WGS84")
                                    {
                                        task.geometry = CoordinateConverter.ConvertGeometryToVN2000(task.geometry, 48);
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
                                var createdTask = await _tasksService.CreateTaskAsync(task);
                                if (createdTask == null)
                                {
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
                                }
                            }
                            catch (Exception ex)
                            {
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
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                ModelState.AddModelError("Task.geometry.type", "Loại hình học phải là 'Point' hoặc 'LineString'.");
            }

            var coordinatesJson = Request.Form["Task.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    Task.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Raw coordinates: {JsonSerializer.Serialize(Task.geometry.coordinates)}");

                    if (GeometrySystem == "wgs84")
                    {
                        Task.geometry = CoordinateConverter.ConvertGeometryToVN2000(Task.geometry, 48);
                        Console.WriteLine($"Converted to VN2000: {JsonSerializer.Serialize(Task.geometry.coordinates)}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Task.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("Task.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                Console.WriteLine($"Creating task: {JsonSerializer.Serialize(Task)}");
                var createdTask = await _tasksService.CreateTaskAsync(Task);
                if (createdTask == null)
                {
                    TempData["Error"] = "Không thể tạo nhiệm vụ. Dữ liệu trả về từ dịch vụ là null.";
                    return Page();
                }

                TempData["Success"] = "Nhiệm vụ đã được tạo thành công!";
                return RedirectToPage("/Tasks/Index");
            }
            catch (JsonException)
            {
                ModelState.AddModelError("Task.geometry.coordinates", "Tọa độ phải là JSON hợp lệ.");
                return Page();
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo nhiệm vụ: {ex.Message}";
                return Page();
            }
        }
    }
}