using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;

        //public IEnumerable<IncidentsResponse> Incidents { get; private set; } = new List<IncidentsResponse>();
        //public int IncidentCount => Incidents.Count();


        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                // Lấy dữ liệu incidents
                var incidents = await _incidentsService.GetAllIncidentsAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Incidents Report");

                    // Thêm header
                    string[] headers = new[] {
                        "Mã Sự cố",
                        "Loại Sự cố",
                        "Mô tả",
                        "Ưu tiên",
                        "Trạng thái",
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
                        worksheet.Cells[row, 2].Value = incident.incident_type;
                        worksheet.Cells[row, 3].Value = incident.description;
                        worksheet.Cells[row, 4].Value = incident.priority;
                        worksheet.Cells[row, 5].Value = incident.status;

                        // Tách tọa độ latitude và longitude từ location.coordinates
                        double? latitude = null;
                        double? longitude = null;
                        if (incident.location?.coordinates != null && incident.location.type == "Point")
                        {
                            // Ghi log chi tiết
                            Console.WriteLine($"Incident {incident.incident_id} - Coordinates Type: {incident.location.coordinates.GetType().Name}");
                            if (incident.location.coordinates is JsonElement jsonElement)
                            {
                                Console.WriteLine($"Incident {incident.incident_id} - Coordinates Raw Value: {jsonElement.ToString()}");
                                Console.WriteLine($"Incident {incident.incident_id} - ValueKind: {jsonElement.ValueKind}");

                                if (jsonElement.ValueKind == JsonValueKind.Array)
                                {
                                    var coords = jsonElement.EnumerateArray().ToArray();
                                    Console.WriteLine($"Incident {incident.incident_id} - Array Length: {coords.Length}");
                                    if (coords.Length >= 2)
                                    {
                                        longitude = coords[0].GetDouble();
                                        latitude = coords[1].GetDouble();
                                        Console.WriteLine($"Incident {incident.incident_id} - Longitude: {longitude}, Latitude: {latitude}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Incident {incident.incident_id} - Coordinates array too short: {coords.Length} elements");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Incident {incident.incident_id} - Coordinates is not an array: {jsonElement.ValueKind}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Incident {incident.incident_id} - Coordinates is not JsonElement");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Incident {incident.incident_id} - No coordinates or not a Point");
                        }

                        worksheet.Cells[row, 6].Value = latitude;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "0.######";
                        worksheet.Cells[row, 7].Value = longitude;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "0.######";

                        //worksheet.Cells[row, 8].Value = incident.created_at?.ToString("dd/MM/yyyy HH:mm");
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
        public IndexModel(IIncidentsService incidentsService)
        {
            _incidentsService = incidentsService;
        }

        public async Task OnGetAsync()
        {
            //Incidents = await _incidentsService.GetAllIncidentsAsync();
        }
    }
}