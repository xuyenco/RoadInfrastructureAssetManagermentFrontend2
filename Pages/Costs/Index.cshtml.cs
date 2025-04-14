using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Costs
{
    public class IndexModel : PageModel
    {
        private readonly ICostsService _costsService;

        public IndexModel(ICostsService costsService)
        {
            _costsService = costsService;
        }

        public List<CostsResponse> Costs { get; set; } = new List<CostsResponse>();

        public async Task OnGetAsync()
        {
            try
            {
                Costs = await _costsService.GetAllCostsAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading costs: {ex.Message}";
                Costs = new List<CostsResponse>();
            }
        }
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                // Lấy dữ liệu costs
                var costs = await _costsService.GetAllCostsAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>"); 

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Costs Report");

                    // Thêm header
                    string[] headers = new[] {
                        "Mã chi phí",
                        "Mã nhiệm vụ",
                        "Loại chi phí",
                        "Tổng số tiền (VNĐ)",
                        "Mô tả chi tiết",
                        "Ngày giải ngân",
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
                    foreach (var cost in costs)
                    {
                        worksheet.Cells[row, 1].Value = cost.cost_id;
                        worksheet.Cells[row, 2].Value = cost.task_id;
                        worksheet.Cells[row, 3].Value = cost.cost_type;
                        worksheet.Cells[row, 4].Value = cost.amount;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 5].Value = cost.description;
                        worksheet.Cells[row, 6].Value = cost.date_incurred;
                        worksheet.Cells[row, 7].Value = cost.created_at;
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Costs_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xuất báo cáo chi phí: {ex.Message}";
                return RedirectToPage(); // Quay lại trang hiện tại nếu có lỗi
            }
        }

        [ValidateAntiForgeryToken] // Thêm bảo mật CSRF
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Console.WriteLine($"Received cost id to delete: {id}");
            try
            {
                var result = await _costsService.DeleteCostAsync(id);
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