using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;

        public IndexModel(IIncidentsService incidentsService)
        {
            _incidentsService = incidentsService;
        }

        public async Task OnGetAsync()
        {
            // No changes needed here as it's commented out
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                // Lấy dữ liệu incidents
                var incidents = await _incidentsService.GetAllIncidentsAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

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

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting incidents: {ex.Message}");
                return RedirectToPage();
            }
        }
    }
}