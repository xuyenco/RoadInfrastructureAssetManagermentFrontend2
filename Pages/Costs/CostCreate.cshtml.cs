using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Costs
{
    public class CostCreateModel : PageModel
    {
        private readonly ICostsService _costsService;

        public CostCreateModel(ICostsService costsService)
        {
            _costsService = costsService;
        }

        [BindProperty]
        public CostsRequest Cost { get; set; } = new CostsRequest();

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

            var createdCost = await _costsService.CreateCostAsync(Cost);
            if (createdCost == null)
            {
                ModelState.AddModelError("", "Không thể tạo Cost. Vui lòng kiểm tra lại dữ liệu.");
                return Page();
            }

            return RedirectToPage("/Costs/Index"); // Chuyển hướng về trang Index sau khi tạo thành công
        }
    }
}
