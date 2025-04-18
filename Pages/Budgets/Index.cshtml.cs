using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Budgets
{
    public class IndexModel : PageModel
    {
        private readonly IBudgetsService _budgetsService;

        public IndexModel(IBudgetsService budgetsService)
        {
            _budgetsService = budgetsService;
        }

        public List<BudgetsResponse> Budgets { get; set; } = new List<BudgetsResponse>();
        public async Task<IActionResult> OnGetExportExcelAsync()  
        {
            try
            {
                // Lấy dữ liệu budgets
                var budgets = await _budgetsService.GetAllBudgetsAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Budgets Report");

                    // Thêm header
                    string[] headers = new[] {
                        "Mã Budget",
                        "Mã Danh mục",
                        "Năm tài chính",
                        "Tổng số tiền (VNĐ)",
                        "Số tiền đã cấp (VNĐ)",
                        "Số tiền còn lại (VNĐ)",
                        "Ngày tạo"
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
                    foreach (var budget in budgets)
                    {
                        worksheet.Cells[row, 1].Value = budget.budget_id;
                        worksheet.Cells[row, 2].Value = budget.cagetory_id;
                        worksheet.Cells[row, 3].Value = budget.fiscal_year;
                        worksheet.Cells[row, 4].Value = budget.total_amount;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 5].Value = budget.allocated_amount;
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Value = budget.remaining_amount;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Value = budget.created_at;
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Budgets_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xuất báo cáo: {ex.Message}";
                return RedirectToPage(); // Quay lại trang hiện tại nếu có lỗi
            }
        }

        public async Task OnGetAsync()
        {
            try
            {
                Budgets = await _budgetsService.GetAllBudgetsAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading budgets: {ex.Message}";
                Budgets = new List<BudgetsResponse>();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Console.WriteLine("delete budget id:" + id);
            try
            {
                var result = await _budgetsService.DeleteBudgetAsync(id);
                return new JsonResult(new { success = true });
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