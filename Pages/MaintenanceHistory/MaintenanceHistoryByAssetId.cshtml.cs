using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    public class MaintenanceHistoryByAssetIdModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;

        public MaintenanceHistoryByAssetIdModel(IMaintenanceHistoryService maintenanceHistoryService, IMaintenanceDocumentService maintenanceDocumentService)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
        }

        public IList<MaintenanceHistoryResponse> MaintenanceHistories { get; set; }
        public IList<MaintenanceDocumentResponse> MaintenanceDocuments { get; set; }
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            try
            {
                // Fetch all MaintenanceHistory records
                MaintenanceHistories = await _maintenanceHistoryService.GetMaintenanceHistoryByAssetId(id);
                if (MaintenanceHistories == null)
                {
                    MaintenanceHistories = new List<MaintenanceHistoryResponse>();
                    TempData["Error"] = "Không thể tải danh sách Lịch sử Bảo trì.";
                    return Page();
                }

                // Fetch all MaintenanceDocument records
                MaintenanceDocuments = new List<MaintenanceDocumentResponse>();
                foreach (var history in MaintenanceHistories)
                {
                    var documents = await _maintenanceDocumentService.GetMaintenanceDocumentByMaintenanceId(history.maintenance_id);
                    if (documents != null)
                    {
                        MaintenanceDocuments = MaintenanceDocuments.Concat(documents).ToList();
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading maintenance histories: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải danh sách Lịch sử Bảo trì: {ex.Message}";
                MaintenanceHistories = new List<MaintenanceHistoryResponse>();
                MaintenanceDocuments = new List<MaintenanceDocumentResponse>();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var temp = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(id);
                if (!temp)
                {
                    TempData["Error"] = "Xóa Tài liệu lịch sử bảo trì thất bại.";
                    return RedirectToPage();
                }
                var deleted = await _maintenanceHistoryService.DeleteMaintenanceHistory(id);
                if (!deleted)
                {
                    TempData["Error"] = "Xóa Lịch sử Bảo trì thất bại.";
                    return RedirectToPage();
                }

                TempData["Success"] = "Xóa Lịch sử Bảo trì thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting maintenance history: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi xóa Lịch sử Bảo trì: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
