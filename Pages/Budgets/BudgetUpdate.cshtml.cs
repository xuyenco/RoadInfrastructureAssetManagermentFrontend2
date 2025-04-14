using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Budgets
{
    public class BudgetUpdateModel : PageModel
    {
        private readonly IBudgetsService _budgetsService;

        public BudgetUpdateModel(IBudgetsService budgetsService)
        {
            _budgetsService = budgetsService;
        }

        [BindProperty]
        public BudgetsRequest BudgetRequest { get; set; } = new BudgetsRequest();

        public BudgetsResponse BudgetResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                BudgetResponse = await _budgetsService.GetBudgetByIdAsync(id);
                if (BudgetResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy Budget với ID này.";
                    return RedirectToPage("/Budgets/Index");
                }

                // Gán giá trị mặc định cho BudgetRequest từ BudgetResponse
                BudgetRequest = new BudgetsRequest
                {
                    cagetory_id = BudgetResponse.cagetory_id,
                    fiscal_year = BudgetResponse.fiscal_year,
                    total_amount = BudgetResponse.total_amount,
                    allocated_amount = BudgetResponse.allocated_amount
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading budget: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Budget: {ex.Message}";
                return RedirectToPage("/Budgets/Index");
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
                Console.WriteLine($"Updating budget with ID: {id}, Category ID: {BudgetRequest.cagetory_id}, Fiscal Year: {BudgetRequest.fiscal_year}");

                // Gửi yêu cầu cập nhật với ID từ query string
                var updatedBudget = await _budgetsService.UpdateBudgetAsync(id, BudgetRequest);
                if (updatedBudget == null)
                {
                    TempData["Error"] = "Không thể cập nhật Budget. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Budget update failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Budget đã được cập nhật thành công!";
                return RedirectToPage("/Budgets/Index");
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
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Budget: {ex.Message}";
                return Page();
            }
        }
    }
}