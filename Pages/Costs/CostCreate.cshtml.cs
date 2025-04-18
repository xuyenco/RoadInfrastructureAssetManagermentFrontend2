using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Costs
{
    public class CostCreateModel : PageModel
    {
        private readonly ICostsService _costsService;

        public CostCreateModel(ICostsService costsService)
        {
            _costsService = costsService;
        }

        [BindProperty]
        public CostsRequest Cost { get; set; } = new CostsRequest();

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }

        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage()) 
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for Cost input");
                var header = new List<string> { "Task id", "Cost type", "Amount", "Description", "Date incurred" };
                for (int i = 0; i < header.Count; i++)
                {
                    worksheet.Cells[1,i+1].Value = header[i];
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Cost Template.xlsx");
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
                var Costs = new List<CostsRequest>();
                var errorRows = new List<ExcelErrorRow>();
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0; // Đặt lại vị trí stream về đầu
                    ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                    using (var package = new ExcelPackage(stream)) // Truyền stream vào đây
                    {
                        var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên
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
                            var cost = new CostsRequest();
                            var rowData = new Dictionary<string, string>();
                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "task id":
                                        cost.task_id = int.TryParse(value, out var taskId) ? taskId : 0;
                                        break;
                                    case "cost type":
                                        cost.cost_type = value;
                                        break;
                                    case "amount":
                                        cost.amount = double.TryParse(value, out var amount) ? amount : 0;
                                        break;
                                    case "description":
                                        cost.description = value;
                                        break;
                                    case "date incurred":
                                        cost.date_incurred = DateTime.TryParse(value, out var dateIncurred) ? dateIncurred : null;
                                        break;
                                }
                            }
                            Costs.Add(cost);
                        }

                        int successCount = 0;
                        for (int i = 0; i < Costs.Count; i++)
                        {
                            var cost = Costs[i];
                            var rowNumber = i + 2;

                            if (cost.task_id == 0 || cost.date_incurred == null || cost.amount == 0)
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(Costs[i]),
                                    ErrorMessage = "Dữ liệu tại task id/date_incurred/amount null hoặc không hợp lệ"
                                });
                                continue;
                            }

                            try
                            {
                                var createCost = await _costsService.CreateCostAsync(cost);
                                if (createCost == null)
                                {
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(cost),
                                        ErrorMessage = "Không thể tạo cost (lỗi từ service)."
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
                                    OriginalData = JsonSerializer.Serialize(cost),
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
                Console.WriteLine($"Creating cost with Task ID: {Cost.task_id}, Cost Type: {Cost.cost_type}");
                var createdCost = await _costsService.CreateCostAsync(Cost);
                if (createdCost == null)
                {
                    TempData["Error"] = "Không thể tạo Cost. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Cost creation failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Cost đã được tạo thành công!";
                return RedirectToPage("/Costs/Index");
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
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo Cost: {ex.Message}";
                return Page();
            }
        }
    }
}