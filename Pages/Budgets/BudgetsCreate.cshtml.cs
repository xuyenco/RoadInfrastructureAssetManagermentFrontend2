using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Budgets
{
    public class BudgetsCreateModel : PageModel
    {
        private readonly IBudgetsService _budgetsService;

        public BudgetsCreateModel(IBudgetsService budgetsService)
        {
            _budgetsService = budgetsService;
        }

        [BindProperty]
        public BudgetsRequest Budget { get; set; } = new BudgetsRequest();

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Trả về trang nếu dữ liệu không hợp lệ
            }

            var createdBudget = await _budgetsService.CreateBudgetAsync(Budget);
            if (createdBudget == null)
            {
                ModelState.AddModelError("", "Không thể tạo Budget. Vui lòng kiểm tra lại dữ liệu.");
                return Page();
            }

            // Chuyển hướng về trang Index sau khi tạo thành công
            return RedirectToPage("/Budgets/Index");
        }
    }
}
