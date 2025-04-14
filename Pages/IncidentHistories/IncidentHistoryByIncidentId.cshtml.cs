using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IncidentHistoryByIncidentIdModel : PageModel
    {
        public readonly IIncidentHistoriesService _incidentHistoriesService;
        public IncidentHistoryByIncidentIdModel (IIncidentHistoriesService incidentHistoriesService)
        {
            _incidentHistoriesService = incidentHistoriesService;
        }
        public List<IncidentHistoriesResponse> IncidentHistorys { get; set; } = new List<IncidentHistoriesResponse>();
        public async Task OnGetAsync(int id)
        {
            try
            {
                IncidentHistorys = await _incidentHistoriesService.GetIncidentHistoriesByIncidentId(id);
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
