using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentHistoriesService _incidentHistoriesService;
        public IndexModel(IIncidentHistoriesService IncidentHistoriesService)
        {
            _incidentHistoriesService = IncidentHistoriesService;
        }
        public List<IncidentHistoriesResponse> IncidentHistorys { get; set; }
        public async Task OnGetAsync()
        {
            IncidentHistorys = await _incidentHistoriesService.GetAllIncidentHistoriesAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _incidentHistoriesService.DeleteIncidentHistoryAsync(id);
            if (result)
            {
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Không thể xóa lịch sử sự cố." });
        }
    }
}
