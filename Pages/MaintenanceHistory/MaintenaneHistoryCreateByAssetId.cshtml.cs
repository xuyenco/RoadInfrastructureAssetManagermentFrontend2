using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.MaintenanceHistory
{
    [AuthorizeRole("admin,inspector")]
    public class MaintenaneHistoryCreateByAssetIdModel : PageModel
    {
        private readonly IMaintenanceHistoryService _maintenanceHistoryService;
        private readonly IMaintenanceDocumentService _maintenanceDocumentService;

        public MaintenaneHistoryCreateByAssetIdModel(IMaintenanceHistoryService maintenanceHistoryService, IMaintenanceDocumentService maintenanceDocumentService)
        {
            _maintenanceHistoryService = maintenanceHistoryService;
            _maintenanceDocumentService = maintenanceDocumentService;
        }

        [BindProperty]
        public MaintenanceHistoryRequest MaintenanceHistory { get; set; } = new MaintenanceHistoryRequest();

        [BindProperty]
        public IFormFile[] Files { get; set; }

        [BindProperty]
        public int id { get; set; }

        public IActionResult OnGet(int id)
        {
            this.id = id; // Store the id for use in the form
            MaintenanceHistory.asset_id = id; // Pre-fill the asset_id field
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            this.id = id; // Ensure id is set for the post action
            MaintenanceHistory.asset_id = id; // Ensure asset_id is set to the passed id

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
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

                // Create MaintenanceHistory
                Console.WriteLine($"Creating maintenance history: {System.Text.Json.JsonSerializer.Serialize(MaintenanceHistory)}");
                var createdHistory = await _maintenanceHistoryService.CreateMaintenanceHistory(MaintenanceHistory);
                if (createdHistory == null)
                {
                    TempData["Error"] = "Tạo lịch sử bảo trì thất bại. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Maintenance history creation failed: null response from service.");
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
                            var createdDocument = await _maintenanceDocumentService.CreateMaintenanceDocument(documentRequest);
                            if (createdDocument == null)
                            {
                                Console.WriteLine($"Failed to create document for maintenance ID {createdHistory.maintenance_id}, file: {file.FileName}");
                                TempData["Warning"] = "Lịch sử bảo trì đã được tạo, nhưng một số tài liệu không thể tải lên.";
                            }
                        }
                    }
                }

                TempData["Success"] = $"Lịch sử bảo trì với ID {createdHistory.maintenance_id} và tài liệu (nếu có) đã được tạo thành công!";
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
                Console.WriteLine($"Argument error: {ex.Message}");
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo lịch sử bảo trì: {ex.Message}";
                return Page();
            }
        }
    }
}