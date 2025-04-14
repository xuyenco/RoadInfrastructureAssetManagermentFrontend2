using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentHistoriesService _incidentHistoriesService;

        public IndexModel(IIncidentHistoriesService incidentHistoriesService)
        {
            _incidentHistoriesService = incidentHistoriesService;
        }

        public List<IncidentHistoriesResponse> IncidentHistorys { get; set; } = new List<IncidentHistoriesResponse>();
        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                // Lấy dữ liệu incident histories
                var histories = await _incidentHistoriesService.GetAllIncidentHistoriesAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>"); 

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Incident Histories Report");

                    // Thêm header
                    string[] headers = new[] {
                        "Mã Lịch sử",
                        "Mã Sự cố",
                        "Mã Nhiệm vụ",
                        "Người thay đổi",
                        "Trạng thái cũ",
                        "Trạng thái mới",
                        "Mô tả thay đổi",
                        "Ngày thay đổi"
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
                    foreach (var history in histories)
                    {
                        worksheet.Cells[row, 1].Value = history.history_id;
                        worksheet.Cells[row, 2].Value = history.incident_id;
                        worksheet.Cells[row, 3].Value = history.task_id;
                        worksheet.Cells[row, 4].Value = history.changed_by;
                        worksheet.Cells[row, 5].Value = history.old_status;
                        worksheet.Cells[row, 6].Value = history.new_status;
                        worksheet.Cells[row, 7].Value = history.change_description;
                        worksheet.Cells[row, 8].Value = history.changed_at.HasValue
                            ? history.changed_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có dữ liệu";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"IncidentHistories_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xuất báo cáo lịch sử sự cố: {ex.Message}";
                return RedirectToPage(); // Quay lại trang hiện tại nếu có lỗi
            }
        }

        public async Task OnGetAsync()
        {
            try
            {
                IncidentHistorys = await _incidentHistoriesService.GetAllIncidentHistoriesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading incident histories: {ex.Message}";
                IncidentHistorys = new List<IncidentHistoriesResponse>();
            }
        }

        [ValidateAntiForgeryToken] // Thêm bảo mật CSRF
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Console.WriteLine($"Received incident history id to delete: {id}");
            try
            {
                var result = await _incidentHistoriesService.DeleteIncidentHistoryAsync(id);
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