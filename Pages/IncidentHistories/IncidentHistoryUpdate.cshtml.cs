using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IncidentHistoryUpdateModel : PageModel
    {
        private readonly IIncidentHistoriesService _incidentHistoriesService;

        public IncidentHistoryUpdateModel(IIncidentHistoriesService incidentHistoriesService)
        {
            _incidentHistoriesService = incidentHistoriesService;
        }

        [BindProperty]
        public IncidentHistoriesRequest IncidentHistoryRequest { get; set; } = new IncidentHistoriesRequest();

        public IncidentHistoriesResponse IncidentHistoryResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                IncidentHistoryResponse = await _incidentHistoriesService.GetIncidentHistoryByIdAsync(id);
                if (IncidentHistoryResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy Incident History với ID này.";
                    return RedirectToPage("/IncidentHistories/Index");
                }

                // Gán giá trị mặc định cho IncidentHistoryRequest từ IncidentHistoryResponse
                IncidentHistoryRequest = new IncidentHistoriesRequest
                {
                    incident_id = IncidentHistoryResponse.incident_id,
                    task_id = IncidentHistoryResponse.task_id,
                    changed_by = IncidentHistoryResponse.changed_by,
                    old_status = IncidentHistoryResponse.old_status,
                    new_status = IncidentHistoryResponse.new_status,
                    change_description = IncidentHistoryResponse.change_description
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading incident history: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Incident History: {ex.Message}";
                return RedirectToPage("/IncidentHistories/Index");
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
                Console.WriteLine($"Updating incident history with ID: {id}, Incident ID: {IncidentHistoryRequest.incident_id}, Task ID: {IncidentHistoryRequest.task_id}");

                // Gửi yêu cầu cập nhật với ID từ query string
                var updatedIncidentHistory = await _incidentHistoriesService.UpdateIncidentHistoryAsync(id, IncidentHistoryRequest);
                if (updatedIncidentHistory == null)
                {
                    TempData["Error"] = "Không thể cập nhật Incident History. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Incident History update failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Incident History đã được cập nhật thành công!";
                return RedirectToPage("/IncidentHistories/Index");
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
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Incident History: {ex.Message}";
                return Page();
            }
        }
    }
}