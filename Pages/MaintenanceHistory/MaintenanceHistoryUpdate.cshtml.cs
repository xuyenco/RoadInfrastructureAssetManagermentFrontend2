using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    public class MaintenanceHistoryUpdateModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;

        public MaintenanceHistoryUpdateModel(IMaintenanceHistoryService maintenanceHistoryService, IMaintenanceDocumentService maintenanceDocumentService)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
        }

        [BindProperty]
        public MaintenanceHistoryRequest MaintenanceHistory { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public IFormFile[] Files { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            var historyResponse = await _maintenanceHistoryService.GetMaintenanceHistoryById(id);
            if (historyResponse == null)
            {
                TempData["Error"] = "Không tìm thấy Lịch sử Bảo trì.";
                return NotFound();
            }

            MaintenanceHistory = new MaintenanceHistoryRequest
            {
                asset_id = historyResponse.asset_id,
                task_id = historyResponse.task_id
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate inputs
                if (MaintenanceHistory.asset_id <= 0)
                {
                    ModelState.AddModelError("MaintenanceHistory.asset_id", "Asset ID phải là số nguyên dương.");
                    return Page();
                }
                if (MaintenanceHistory.task_id <= 0)
                {
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
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} không hợp lệ. Chỉ chấp nhận JPEG, PNG, PDF, DOC, hoặc DOCX.");
                                return Page();
                            }
                            if (file.Length > 10 * 1024 * 1024) // 10MB limit
                            {
                                ModelState.AddModelError("Files", $"Tệp {file.FileName} vượt quá giới hạn 10MB.");
                                return Page();
                            }
                        }
                    }
                }

                // Log the update request
                Console.WriteLine($"Updating maintenance history: {System.Text.Json.JsonSerializer.Serialize(MaintenanceHistory)}");

                var temp = await _maintenanceDocumentService.DeleteMaintenanceDocumentByMaintenanceId(Id);
                if (!temp)
                {
                    TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Lịch sử Bảo trì: Không thể xóa tài liệu cũ liên quan";
                    return Page();
                }

                // Update MaintenanceHistory
                var updatedHistory = await _maintenanceHistoryService.UpdateMaintenanceHistory(Id, MaintenanceHistory);
                if (updatedHistory == null)
                {
                    TempData["Error"] = "Cập nhật Lịch sử Bảo trì thất bại.";
                    Console.WriteLine("Maintenance history update failed: null response from service.");
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
                            var createdDocument = await _maintenanceDocumentService.CreateMaintenanceDocument(documentRequest);
                            if (createdDocument == null)
                            {
                                Console.WriteLine($"Failed to create document for maintenance ID {updatedHistory.maintenance_id}, file: {file.FileName}");
                                TempData["Warning"] = "Lịch sử Bảo trì đã được cập nhật, nhưng một số tài liệu không thể thêm.";
                            }
                        }
                    }
                }

                TempData["Success"] = "Lịch sử Bảo trì và tài liệu (nếu có) đã được cập nhật thành công!";
                return RedirectToPage("/MaintenanceHistory/Index");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Unauthorized access: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Login");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Validation error: {ex.Message}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating maintenance history: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Lịch sử Bảo trì: {ex.Message}";
                return Page();
            }
        }
    }
}
