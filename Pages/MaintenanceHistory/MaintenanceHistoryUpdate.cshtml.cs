using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    [AuthorizeRole("admin,inspector")]
    public class MaintenanceHistoryUpdateModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;
        private readonly ILogger<MaintenanceHistoryUpdateModel> _logger;

        public MaintenanceHistoryUpdateModel(
            IMaintenanceHistoryService maintenanceHistoryService,
            IMaintenanceDocumentService maintenanceDocumentService,
            ILogger<MaintenanceHistoryUpdateModel> logger)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
            _logger = logger;
        }

        [BindProperty]
        public MaintenanceHistoryRequest MaintenanceHistory { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public IFormFile[] Files { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving maintenance history with ID {MaintenanceId} for update", username, role, id);

            Id = id;
            var historyResponse = await _maintenanceHistoryService.GetMaintenanceHistoryById(id);
            if (historyResponse == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no maintenance history with ID {MaintenanceId}", username, role, id);
                TempData["Error"] = "Không tìm thấy Lịch sử Bảo trì.";
                return NotFound();
            }

            MaintenanceHistory = new MaintenanceHistoryRequest
            {
                asset_id = historyResponse.asset_id,
                task_id = historyResponse.task_id
            };

            _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved maintenance history with ID {MaintenanceId}", username, role, id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting update for maintenance history with ID {MaintenanceId}", username, role, Id);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}", username, role, JsonSerializer.Serialize(Request.Form));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors for maintenance history with ID {MaintenanceId}: {Errors}", username, role, Id, JsonSerializer.Serialize(errors));
                return Page();
            }

            try
            {
                // Validate inputs
                if (MaintenanceHistory.asset_id <= 0)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid asset_id: {AssetId} for maintenance history with ID {MaintenanceId}", username, role, MaintenanceHistory.asset_id, Id);
                    ModelState.AddModelError("MaintenanceHistory.asset_id", "Asset ID phải là số nguyên dương.");
                    return Page();
                }
                if (MaintenanceHistory.task_id <= 0)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid task_id: {TaskId} for maintenance history with ID {MaintenanceId}", username, role, MaintenanceHistory.task_id, Id);
                    ModelState.AddModelError("MaintenanceHistory.task_id", "Task ID phải là số nguyên dương.");
                    return Page();
                }
                if (Files != null && Files.Length > 0)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                    foreach (var file in Files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            if (!allowedTypes.Contains(file.ContentType))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) uploaded invalid file type for {FileName}: {ContentType} for maintenance history with ID {MaintenanceId}", username, role, file.FileName, file.ContentType, Id);
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} không hợp lệ. Chỉ chấp nhận JPEG, PNG, PDF, DOC, hoặc DOCX.");
                                return Page();
                            }
                            if (file.Length > 10 * 1024 * 1024) // 10MB limit
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) uploaded file {FileName} exceeding size limit: {Size} bytes for maintenance history with ID {MaintenanceId}", username, role, file.FileName, file.Length, Id);
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} vượt quá giới hạn 10MB.");
                                return Page();
                            }
                        }
                    }
                }

                var currentMaintenanceDocument = await _maintenanceDocumentService.GetMaintenanceDocumentByMaintenanceId(Id);
                if (currentMaintenanceDocument != null)
                {
                    // Delete existing documents
                    _logger.LogDebug("User {Username} (Role: {Role}) deleting existing documents for maintenance history with ID {MaintenanceId}", username, role, Id);
                    var documentsDeleted = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(Id);
                    if (!documentsDeleted)
                    {
                        _logger.LogWarning("User {Username} (Role: {Role}) failed to delete existing documents for maintenance history with ID {MaintenanceId}", username, role, Id);
                        TempData["Error"] = "Đã xảy ra lỗi khi cập nhật Lịch sử Bảo trì: Không thể xóa tài liệu cũ liên quan";
                        return Page();
                    }
                }

                // Update MaintenanceHistory
                _logger.LogDebug("User {Username} (Role: {Role}) updating maintenance history with ID {MaintenanceId}: {HistoryData}", username, role, Id, JsonSerializer.Serialize(MaintenanceHistory));
                var updatedHistory = await _maintenanceHistoryService.UpdateMaintenanceHistory(Id, MaintenanceHistory);
                if (updatedHistory == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to update maintenance history with ID {MaintenanceId}: No result returned", username, role, Id);
                    TempData["Error"] = "Cập nhật Lịch sử Bảo trì thất bại.";
                    return Page();
                }

                // Create MaintenanceDocuments if files are provided
                if (Files != null && Files.Length > 0)
                {
                    foreach (var file in Files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var documentRequest = new MaintenanceDocumentRequest
                            {
                                maintenance_id = updatedHistory.maintenance_id,
                                file = file
                            };
                            _logger.LogDebug("User {Username} (Role: {Role}) uploading document for maintenance ID {MaintenanceId}: filename={FileName}, size={Size}", username, role, updatedHistory.maintenance_id, file.FileName, file.Length);
                            var createdDocument = await _maintenanceDocumentService.CreateMaintenanceDocument(documentRequest);
                            if (createdDocument == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to upload document for maintenance ID {MaintenanceId}: filename={FileName}", username, role, updatedHistory.maintenance_id, file.FileName);
                                TempData["Warning"] = "Lịch sử Bảo trì đã được cập nhật, nhưng một số tài liệu không thể thêm.";
                            }
                        }
                    }
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully updated maintenance history with ID {MaintenanceId} and uploaded {DocumentCount} documents", username, role, updatedHistory.maintenance_id, Files?.Length ?? 0);
                TempData["Success"] = "Lịch sử Bảo trì và tài liệu (nếu có) đã được cập nhật thành công!";
                return RedirectToPage("/MaintenanceHistory/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access for maintenance history with ID {MaintenanceId}: {Error}", username, role, Id, ex.Message);
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error for maintenance history with ID {MaintenanceId}: {Error}", username, role, Id, ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error updating maintenance history with ID {MaintenanceId}: {Error}", username, role, Id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Lịch sử Bảo trì: {ex.Message}";
                return Page();
            }
        }
    }
}