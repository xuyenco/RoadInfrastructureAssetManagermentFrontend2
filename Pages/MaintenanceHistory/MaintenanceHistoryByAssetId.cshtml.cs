using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    [AuthorizeRole("admin,inspector")]
    public class MaintenanceHistoryByAssetIdModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;
        private readonly ILogger<MaintenanceHistoryByAssetIdModel> _logger;

        public MaintenanceHistoryByAssetIdModel(
            IMaintenanceHistoryService maintenanceHistoryService,
            IMaintenanceDocumentService maintenanceDocumentService,
            ILogger<MaintenanceHistoryByAssetIdModel> logger)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
            _logger = logger;
        }

        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the maintenance history by asset id page for asset ID {AssetId}",
                username, role, id);

            Id = id;
            return Page();
        }

        public async Task<IActionResult> OnGetHistoryAsync(int id, int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            _logger.LogDebug("OnGetHistoryAsync called with id: {Id}, currentPage: {CurrentPage}, pageSize: {PageSize}, searchTerm: '{SearchTerm}', searchField: {SearchField}",
                id, currentPage, pageSize, searchTerm, searchField);

            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation(
                "User {Username} (Role: {Role}) is fetching paged maintenance histories for asset ID {AssetId} with parameters: Page={CurrentPage}, PageSize={PageSize}, SearchTerm='{SearchTerm}', SearchField={SearchField}",
                username, role, id, currentPage, pageSize, searchTerm, searchField);

            try
            {
                var pagedResult = await _maintenanceHistoryService.GetPagedMaintenanceHistoryByAssetId(id, currentPage, pageSize, searchTerm, searchField);
                if (pagedResult == null || pagedResult.maintenanceHistories == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching paged maintenance histories for asset ID {AssetId}",
                        username, role, id);
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Không thể tải danh sách Lịch sử Bảo trì."
                    });
                }

                // Fetch MaintenanceDocuments for the current page's maintenance histories
                var maintenanceDocuments = new List<MaintenanceDocumentResponse>();
                foreach (var history in pagedResult.maintenanceHistories)
                {
                    var documents = await _maintenanceDocumentService.GetMaintenanceDocumentByMaintenanceId(history.maintenance_id);
                    if (documents != null)
                    {
                        maintenanceDocuments.AddRange(documents);
                    }
                }

                _logger.LogInformation(
                    "User {Username} (Role: {Role}) retrieved {HistoryCount} maintenance histories and {DocumentCount} documents for asset ID {AssetId} (Page: {CurrentPage}, TotalCount: {TotalCount})",
                    username, role, pagedResult.maintenanceHistories.Count, maintenanceDocuments.Count, id, currentPage, pagedResult.totalCount);

                return new JsonResult(new
                {
                    success = true,
                    maintenanceHistories = pagedResult.maintenanceHistories,
                    maintenanceDocuments,
                    totalCount = pagedResult.totalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "User {Username} (Role: {Role}) encountered error fetching paged maintenance histories for asset ID {AssetId}: {Error}",
                    username, role, id, ex.Message);
                return new JsonResult(new
                {
                    success = false,
                    message = $"Đã xảy ra lỗi khi tải danh sách Lịch sử Bảo trì: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete maintenance history with ID {MaintenanceId}",
                username, role, id);

            try
            {
                var temp = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(id);
                if (!temp)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete documents for maintenance history with ID {MaintenanceId}",
                        username, role, id);
                    TempData["Error"] = "Xóa Tài liệu lịch sử bảo trì thất bại.";
                    return RedirectToPage();
                }
                _logger.LogDebug("User {Username} (Role: {Role}) successfully deleted documents for maintenance history with ID {MaintenanceId}",
                    username, role, id);

                var deleted = await _maintenanceHistoryService.DeleteMaintenanceHistory(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete maintenance history with ID {MaintenanceId}",
                        username, role, id);
                    TempData["Error"] = "Xóa Lịch sử Bảo trì thất bại.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted maintenance history with ID {MaintenanceId}",
                    username, role, id);
                TempData["Success"] = "Xóa Lịch sử Bảo trì thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting maintenance history with ID {MaintenanceId}: {Error}",
                    username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi xóa Lịch sử Bảo trì: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}