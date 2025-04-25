using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using Microsoft.Extensions.Logging;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    public class IndexModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IMaintenanceHistoryService maintenanceHistoryService, IMaintenanceDocumentService maintenanceDocumentService, ILogger<IndexModel> logger)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
            _logger = logger;
        }

        public IList<MaintenanceHistoryResponse> MaintenanceHistories { get; set; }
        public IList<MaintenanceDocumentResponse> MaintenanceDocuments { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the maintenance history index page", username, role);

            try
            {
                // Fetch all MaintenanceHistory records
                MaintenanceHistories = await _maintenanceHistoryService.GetAllMaintenanceHistories();
                if (MaintenanceHistories == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching maintenance histories", username, role);
                    MaintenanceHistories = new List<MaintenanceHistoryResponse>();
                    TempData["Error"] = "Không thể tải danh sách Lịch sử Bảo trì.";
                    return Page();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {HistoryCount} maintenance histories",username, role, MaintenanceHistories.Count);

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
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {DocumentCount} maintenance documents",username, role, MaintenanceDocuments.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading maintenance histories: {Error}",username, role, ex.Message);
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

            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete maintenance history with ID {MaintenanceId}",username, role, id);

            try
            {
                // Delete associated documents
                var documentsDeleted = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(id);
                if (!documentsDeleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete documents for maintenance history with ID {MaintenanceId}",username, role, id);
                    TempData["Error"] = "Xóa Tài liệu lịch sử bảo trì thất bại.";
                    return RedirectToPage();
                }
                _logger.LogDebug("User {Username} (Role: {Role}) successfully deleted documents for maintenance history with ID {MaintenanceId}",
                    username, role, id);

                // Delete maintenance history
                var historyDeleted = await _maintenanceHistoryService.DeleteMaintenanceHistory(id);
                if (!historyDeleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete maintenance history with ID {MaintenanceId}",
                        username, role, id);
                    TempData["Error"] = "Xóa Lịch sử Bảo trì thất bại.";
                    return RedirectToPage();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted maintenance history with ID {MaintenanceId}",username, role, id);
                TempData["Success"] = "Xóa Lịch sử Bảo trì thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting maintenance history with ID {MaintenanceId}: {Error}",username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi xóa Lịch sử Bảo trì: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}