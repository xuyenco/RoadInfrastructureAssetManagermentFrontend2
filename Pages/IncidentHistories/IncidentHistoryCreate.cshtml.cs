using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.IncidentHistories
{
    public class IncidentHistoryCreateModel : PageModel
    {
        private readonly IIncidentHistoriesService _incidentHistoriesService;

        public IncidentHistoryCreateModel(IIncidentHistoriesService incidentHistoriesService)
        {
            _incidentHistoriesService = incidentHistoriesService;
        }

        [BindProperty]
        public IncidentHistoriesRequest IncidentHistory { get; set; } = new IncidentHistoriesRequest();

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

            var createdHistory = await _incidentHistoriesService.CreateIncidentHistoryAsync(IncidentHistory);
            if (createdHistory == null)
            {
                ModelState.AddModelError("", "Không thể tạo Incident History. Vui lòng kiểm tra lại dữ liệu.");
                return Page();
            }

            return RedirectToPage("/IncidentHistories/Index"); // Chuyển hướng về trang Index sau khi tạo thành công
        }
    }
}
