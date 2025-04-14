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

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IncidentHistoryCreateModel : PageModel
    {
        private readonly IIncidentHistoriesService _incidentHistoriesService;

        public IncidentHistoryCreateModel(IIncidentHistoriesService incidentHistoriesService)
        {
            _incidentHistoriesService = incidentHistoriesService;
        }

        [BindProperty]
        public IncidentHistoriesRequest IncidentHistory { get; set; } = new IncidentHistoriesRequest();

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }

        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for Incident History input");
                var header = new List<string> { "Incident Id", "Task Id", "Changed By", "Old Status", "New Status", "Change Description" };
                for (int i = 0; i < header.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = header[i];
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Incident History Template.xlsx");
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
                var IncidentHistories = new List<IncidentHistoriesRequest>();
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
                            var incidentHistory = new IncidentHistoriesRequest();
                            var rowData = new Dictionary<string, string>();
                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "incident id":
                                        incidentHistory.incident_id = int.TryParse(value, out var calId) ? calId : 0;
                                        break;
                                    case "task id":
                                        incidentHistory.task_id = int.TryParse(value, out var taskid) ? taskid : 0;
                                        break;
                                    case "changed by":
                                        incidentHistory.changed_by = int.TryParse(value, out var fisyear) ? fisyear : 0;
                                        break;
                                    case "old status":
                                        incidentHistory.old_status = value;
                                        break;
                                    case "new status":
                                        incidentHistory.new_status  = value;
                                        break;
                                    case "change description":
                                        incidentHistory.change_description = value;
                                        break;
                                }
                            }
                            IncidentHistories.Add(incidentHistory);
                        }

                        int successCount = 0;
                        for (int i = 0; i < IncidentHistories.Count; i++)
                        {
                            var incidentHistory = IncidentHistories[i];
                            var rowNumber = i + 2;

                            try
                            {
                                var createIncidentHistory = await _incidentHistoriesService.CreateIncidentHistoryAsync(incidentHistory);
                                if (createIncidentHistory == null)
                                {
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(incidentHistory),
                                        ErrorMessage = "Không thể tạo incidentHistory (lỗi từ service)."
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
                                    OriginalData = JsonSerializer.Serialize(incidentHistory),
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
                Console.WriteLine($"Creating incident history with Incident ID: {IncidentHistory.incident_id}, Task ID: {IncidentHistory.task_id}");
                var createdHistory = await _incidentHistoriesService.CreateIncidentHistoryAsync(IncidentHistory);
                if (createdHistory == null)
                {
                    TempData["Error"] = "Không thể tạo Incident History. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Incident History creation failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Incident History đã được tạo thành công!";
                return RedirectToPage("/IncidentHistories/Index");
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
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Incident History: {ex.Message}";
                return Page();
            }
        }
    }
}