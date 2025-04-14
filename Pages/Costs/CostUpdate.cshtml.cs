using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Costs
{
    public class CostUpdateModel : PageModel
    {
        private readonly ICostsService _costsService;

        public CostUpdateModel(ICostsService costsService)
        {
            _costsService = costsService;
        }

        [BindProperty]
        public CostsRequest CostRequest { get; set; } = new CostsRequest();

        public CostsResponse CostResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                CostResponse = await _costsService.GetCostByIdAsync(id);
                if (CostResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy Cost với ID này.";
                    return RedirectToPage("/Costs/Index");
                }

                // Gán giá trị mặc định cho CostRequest từ CostResponse
                CostRequest = new CostsRequest
                {
                    task_id = CostResponse.task_id,
                    cost_type = CostResponse.cost_type,
                    amount = CostResponse.amount,
                    description = CostResponse.description,
                    date_incurred = CostResponse.date_incurred
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cost: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Cost: {ex.Message}";
                return RedirectToPage("/Costs/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
                return Page(); // Trả về trang nếu dữ liệu không hợp lệ
            }

            try
            {
                Console.WriteLine($"Updating cost with ID: {id}, Task ID: {CostRequest.task_id}, Cost Type: {CostRequest.cost_type}");

                // Gửi yêu cầu cập nhật với ID từ query string
                var updatedCost = await _costsService.UpdateCostAsync(id, CostRequest);
                if (updatedCost == null)
                {
                    TempData["Error"] = "Không thể cập nhật Cost. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Cost update failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Cost đã được cập nhật thành công!";
                return RedirectToPage("/Costs/Index");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Cost: {ex.Message}";
                return Page();
            }
        }
    }
}