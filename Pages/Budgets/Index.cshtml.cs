using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Budgets
{
    public class IndexModel : PageModel
    {
        private readonly IBudgetsService _budgetsService;
        public IndexModel(IBudgetsService budgetsService)
        {
            _budgetsService = budgetsService;
        }
        public List<BudgetsResponse> Budgets {  get; set; }
        public async Task OnGetAsync()
        {
            Budgets = await _budgetsService.GetAllBudgetsAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _budgetsService.DeleteBudgetAsync(id);
            if (result)
            {
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Không thể xóa budgets." });
        }
    }
}
