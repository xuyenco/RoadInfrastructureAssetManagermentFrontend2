using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    //[AuthorizeRole("inspector")]
    public class MaintenanceHistoryCreateModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;
        private readonly ILogger<MaintenanceHistoryCreateModel> _logger;

        public MaintenanceHistoryCreateModel(
            IMaintenanceHistoryService maintenanceHistoryService,
            IMaintenanceDocumentService maintenanceDocumentService,
            ILogger<MaintenanceHistoryCreateModel> logger)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
            _logger = logger;
        }

        [BindProperty]
        public MaintenanceHistoryRequest MaintenanceHistory { get; set; } = new MaintenanceHistoryRequest();

        [BindProperty]
        public IFormFile[] Files { get; set; }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the maintenance history creation page", username, role);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new maintenance history", username, role);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}",
                username, role, JsonSerializer.Serialize(Request.Form));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors: {Errors}",
                    username, role, JsonSerializer.Serialize(errors));
                return Page();
            }

            try
            {
                // Validate inputs
                if (MaintenanceHistory.asset_id <= 0)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid asset_id: {AssetId}",
                        username, role, MaintenanceHistory.asset_id);
                    ModelState.AddModelError("MaintenanceHistory.asset_id", "Asset ID phải là số nguyên dương.");
                    return Page();
                }
                if (MaintenanceHistory.task_id <= 0)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid task_id: {TaskId}",
                        username, role, MaintenanceHistory.task_id);
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
                                _logger.LogWarning("User {Username} (Role: {Role}) uploaded invalid file type for {FileName}: {ContentType}",
                                    username, role, file.FileName, file.ContentType);
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} không hợp lệ. Chỉ chấp nhận JPEG, PNG, PDF, DOC, hoặc DOCX.");
                                return Page();
                            }
                            if (file.Length > 10 * 1024 * 1024) // 10MB limit
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) uploaded file {FileName} exceeding size limit: {Size} bytes",
                                    username, role, file.FileName, file.Length);
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} vượt quá giới hạn 10MB.");
                                return Page();
                            }
                        }
                    }
                }

                // Create MaintenanceHistory
                _logger.LogDebug("User {Username} (Role: {Role}) creating maintenance history: {HistoryData}",
                    username, role, JsonSerializer.Serialize(MaintenanceHistory));
                var createdHistory = await _maintenanceHistoryService.CreateMaintenanceHistory(MaintenanceHistory);
                if (createdHistory == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create maintenance history: No result returned",
                        username, role);
                    TempData["Error"] = "Tạo lịch sử bảo trì thất bại. Dữ liệu trả về từ dịch vụ là null.";
                    return Page();
                }

                // Create MaintenanceDocuments for each file
                if (Files != null && Files.Length > 0)
                {
                    foreach (var file in Files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var documentRequest = new MaintenanceDocumentRequest
                            {
                                maintenance_id = createdHistory.maintenance_id,
                                file = file
                            };
                            _logger.LogDebug("User {Username} (Role: {Role}) uploading document for maintenance ID {MaintenanceId}: filename={FileName}, size={Size}",
                                username, role, createdHistory.maintenance_id, file.FileName, file.Length);
                            var createdDocument = await _maintenanceDocumentService.CreateMaintenanceDocument(documentRequest);
                            if (createdDocument == null)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) failed to upload document for maintenance ID {MaintenanceId}: filename={FileName}",
                                    username, role, createdHistory.maintenance_id, file.FileName);
                                TempData["Warning"] = "Lịch sử bảo trì đã được tạo, nhưng một số tài liệu không thể tải lên.";
                            }
                        }
                    }
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully created maintenance history with ID {MaintenanceId} and uploaded {DocumentCount} documents",
                    username, role, createdHistory.maintenance_id, Files?.Length ?? 0);
                TempData["Success"] = $"Lịch sử bảo trì với ID {createdHistory.maintenance_id} và tài liệu (nếu có) đã được tạo thành công!";
                return RedirectToPage("/MaintenanceHistory/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access: {Error}",
                    username, role, ex.Message);
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error: {Error}", username, role, ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error creating maintenance history: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo lịch sử bảo trì: {ex.Message}";
                return Page();
            }
        }
    }
}