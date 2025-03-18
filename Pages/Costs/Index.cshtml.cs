using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Costs
{
    public class IndexModel : PageModel
    {
        private readonly ICostsService _costsService;
        public IndexModel(ICostsService costsService)
        {
            _costsService = costsService;
        }
        public List<CostsResponse> Costs { get; set; }
        public async Task OnGetAsync()
        {
            Costs = await _costsService.GetAllCostsAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _costsService.DeleteCostAsync(id);
            if (result)
            {
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Không thể xóa chi phí." });
        }
    }
}
