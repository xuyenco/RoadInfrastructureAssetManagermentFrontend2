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
        private readonly ILogger _logger;

        public MaintenanceHistoryByAssetIdModel(IMaintenanceHistoryService maintenanceHistoryService, IMaintenanceDocumentService maintenanceDocumentService, ILogger logger)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
            _logger = logger;
        }

        public IList<MaintenanceHistoryResponse> MaintenanceHistories { get; set; }
        public IList<MaintenanceDocumentResponse> MaintenanceDocuments { get; set; }
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the maintenance history by asset id page", username, role);

            Id = id;
            try
            {
                // Fetch all MaintenanceHistory records
                MaintenanceHistories = await _maintenanceHistoryService.GetMaintenanceHistoryByAssetId(id);
                if (MaintenanceHistories == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching maintenance histories by asset id : {Id}", username, role, id);
                    MaintenanceHistories = new List<MaintenanceHistoryResponse>();
                    TempData["Error"] = "Không thể tải danh sách Lịch sử Bảo trì.";
                    return Page();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {HistoryCount} maintenance histories by asset id : {Id}", username, role, MaintenanceHistories.Count, id);

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
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {DocumentCount} maintenance documents by asset id: {Id}", username, role, MaintenanceDocuments.Count, id);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading maintenance histories by asset id {AssetId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tải danh sách Lịch sử Bảo trì: {ex.Message}";
                MaintenanceHistories = new List<MaintenanceHistoryResponse>();
                MaintenanceDocuments = new List<MaintenanceDocumentResponse>();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete maintenance history with ID {MaintenanceId}", username, role, id);

            try
            {
                var temp = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(id);
                if (!temp)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete documents for maintenance history with ID {MaintenanceId}", username, role, id);
                    TempData["Error"] = "Xóa Tài liệu lịch sử bảo trì thất bại.";
                    return RedirectToPage();
                }
                _logger.LogDebug("User {Username} (Role: {Role}) successfully deleted documents for maintenance history with ID {MaintenanceId}",username, role, id);

                var deleted = await _maintenanceHistoryService.DeleteMaintenanceHistory(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete maintenance history with ID {MaintenanceId}",username, role, id);
                    TempData["Error"] = "Xóa Lịch sử Bảo trì thất bại.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted maintenance history with ID {MaintenanceId}", username, role, id);
                TempData["Success"] = "Xóa Lịch sử Bảo trì thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting maintenance history with ID {MaintenanceId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi xóa Lịch sử Bảo trì: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
