using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IncidentCreateModel : PageModel
    {
        [BindProperty]
        public IncidentsRequest Incident { get; set; } = new IncidentsRequest();

        private readonly IIncidentsService _incidentsService;

        public IncidentCreateModel(IIncidentsService incidentsService)
        {
            _incidentsService = incidentsService;
        }

        public IActionResult OnGet()
        {
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Xử lý location.type
            var locationType = Request.Form["Incident.location.type"].ToString();
            if (!string.IsNullOrEmpty(locationType))
            {
                Incident.location.type = locationType;
            }
            else
            {
                ModelState.AddModelError("Incident.location.type", "Loại hình học là bắt buộc.");
            }

            // 2. Xử lý location.coordinates
            var coordinatesJson = Request.Form["Incident.location.coordinates"].ToString();
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    // Deserialize coordinates thành object
                    Incident.location.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Deserialized coordinates: {JsonSerializer.Serialize(Incident.location.coordinates)}");

                    // Xóa lỗi cũ trong ModelState liên quan đến coordinates
                    if (ModelState.ContainsKey("Incident.location.coordinates"))
                    {
                        ModelState["Incident.location.coordinates"].Errors.Clear();
                        ModelState["Incident.location.coordinates"].ValidationState = ModelValidationState.Valid;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Incident.location.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("Incident.location.coordinates", "Tọa độ là bắt buộc.");
            }

            // 3. Kiểm tra ModelState sau khi xử lý
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                return Page();
            }

            // 4. Gửi yêu cầu tạo incident
            try
            {
                var createdIncident = await _incidentsService.CreateIncidentAsync(Incident);
                if (createdIncident != null)
                {
                    return RedirectToPage("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Không thể tạo incident.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return Page();
            }
        }
    }
}
