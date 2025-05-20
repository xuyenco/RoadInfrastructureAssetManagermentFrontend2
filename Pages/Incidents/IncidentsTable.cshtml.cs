using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Drawing;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IncidentsTableModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly ILogger<IncidentsTableModel> _logger;

        public IncidentsTableModel(IIncidentsService incidentsService, ILogger<IncidentsTableModel> logger)
        {
            _incidentsService = incidentsService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the incidents table page", username, role);
            return Page();
        }

        public async Task<IActionResult> OnGetIncidentsAsync(int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is fetching incidents - Page: {CurrentPage}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, currentPage, pageSize, searchTerm, searchField);

            try
            {
                var (incidents, totalCount) = await _incidentsService.GetIncidentsAsync(currentPage, pageSize, searchTerm, searchField);
                if (incidents == null || !incidents.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received empty incidents list for Page: {CurrentPage}", username, role, currentPage);
                    return new JsonResult(new { success = true, incidents = new List<IncidentsResponse>(), totalCount = 0 });
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {IncidentCount} incidents with total count {TotalCount} for Page: {CurrentPage}",
                    username, role, incidents.Count, totalCount, currentPage);
                return new JsonResult(new { success = true, incidents, totalCount });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access while fetching incidents", username, role);
                HttpContext.Session.Clear();
                return new JsonResult(new { success = false, message = "Phiên làm việc hết hạn, vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error fetching incidents: {Error}", username, role, ex.Message);
                return new JsonResult(new { success = false, message = $"Lỗi khi tải danh sách sự cố: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete incident with ID {IncidentId}", username, role, id);

            try
            {
                var deleted = await _incidentsService.DeleteIncidentAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete incident with ID {IncidentId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Xóa sự cố thất bại." });
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted incident with ID {IncidentId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting incident with ID {IncidentId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting incident with ID {IncidentId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi khi xóa sự cố: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting incidents to Excel", username, role);

            try
            {
                var incidents = await _incidentsService.GetAllIncidentsAsync();
                if (incidents == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching incidents for export", username, role);
                    TempData["Error"] = "Không thể tải danh sách sự cố để xuất Excel.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {IncidentCount} incidents for export", username, role, incidents.Count);

                ExcelPackage.License.SetNonCommercialPersonal("Duong");
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Incidents Report");

                    string[] headers = new[]
                    {
                        "Mã sự cố",
                        "Địa chỉ",
                        "Tuyến đường",
                        "Mức độ nghiêm trọng",
                        "Mức độ hư hại",
                        "Trạng thái xử lý",
                        "Mã nhiệm vụ",
                        "Ngày tạo"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    int row = 2;
                    foreach (var incident in incidents)
                    {
                        worksheet.Cells[row, 1].Value = incident.incident_id;
                        worksheet.Cells[row, 2].Value = incident.address;
                        worksheet.Cells[row, 3].Value = incident.route;
                        worksheet.Cells[row, 4].Value = incident.severity_level;
                        worksheet.Cells[row, 5].Value = incident.damage_level;
                        worksheet.Cells[row, 6].Value = incident.processing_status;
                        worksheet.Cells[row, 7].Value = incident.task_id.HasValue ? incident.task_id.Value.ToString() : "Không có";
                        worksheet.Cells[row, 8].Value = incident.created_at.HasValue
                            ? incident.created_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có dữ liệu";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Incidents_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {IncidentCount} incidents to Excel file {FileName}", username, role, incidents.Count, fileName);
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting incidents to Excel: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo sự cố: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}