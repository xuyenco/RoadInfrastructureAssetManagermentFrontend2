using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Incidents
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IIncidentsService incidentsService, ILogger<IndexModel> logger)
        {
            _incidentsService = incidentsService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the incidents index page", username, role);
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting incidents to Excel", username, role);

            try
            {
                // Lấy dữ liệu incidents
                var incidents = await _incidentsService.GetAllIncidentsAsync();
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {IncidentCount} incidents for export",username, role, incidents.Count);

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Incidents Report");

                    // Thêm header
                    string[] headers = new[]
                    {
                        "Mã Sự cố",
                        "Địa chỉ",
                        "Tuyến đường",
                        "Mức độ nghiêm trọng",
                        "Mức độ hư hỏng",
                        "Trạng thái xử lý",
                        "Mã nhiệm vụ",
                        "Latitude",
                        "Longitude",
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
                    foreach (var incident in incidents)
                    {
                        worksheet.Cells[row, 1].Value = incident.incident_id;
                        worksheet.Cells[row, 2].Value = incident.address;
                        worksheet.Cells[row, 3].Value = incident.route;
                        worksheet.Cells[row, 4].Value = incident.severity_level;
                        worksheet.Cells[row, 5].Value = incident.damage_level;
                        worksheet.Cells[row, 6].Value = incident.processing_status;
                        worksheet.Cells[row, 7].Value = incident.task_id;

                        // Tách tọa độ latitude và longitude từ geometry.coordinates
                        double? latitude = null;
                        double? longitude = null;
                        if (incident.geometry?.coordinates != null && incident.geometry.type == "Point")
                        {
                            if (incident.geometry.coordinates is JsonElement jsonElement)
                            {
                                if (jsonElement.ValueKind == JsonValueKind.Array)
                                {
                                    var coords = jsonElement.EnumerateArray().ToArray();
                                    if (coords.Length >= 2)
                                    {
                                        longitude = coords[0].GetDouble();
                                        latitude = coords[1].GetDouble();
                                    }
                                }
                            }
                            else if (incident.geometry.coordinates is double[] coords && coords.Length >= 2)
                            {
                                longitude = coords[0];
                                latitude = coords[1];
                            }
                        }

                        worksheet.Cells[row, 8].Value = latitude;
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "0.######";
                        worksheet.Cells[row, 9].Value = longitude;
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "0.######";
                        worksheet.Cells[row, 10].Value = incident.created_at?.ToString("dd/MM/yyyy HH:mm");

                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Incidents_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {IncidentCount} incidents to Excel file {FileName}",username, role, incidents.Count, fileName);

                    return File(stream,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting incidents: {Error}",username, role, ex.Message);
                return RedirectToPage();
            }
        }
    }
}