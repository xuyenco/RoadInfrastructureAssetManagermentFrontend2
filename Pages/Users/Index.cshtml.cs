using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUsersService _usersService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IUsersService usersService, ILogger<IndexModel> logger)
        {
            _usersService = usersService;
            _logger = logger;
        }

        public List<UsersResponse> Users { get; set; } = new List<UsersResponse>();

        public async Task<IActionResult> OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the users index page", username, role);

            try
            {
                Users = await _usersService.GetAllUsersAsync();
                if (Users == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching users", username, role);
                    TempData["Error"] = "Không thể tải danh sách người dùng.";
                    Users = new List<UsersResponse>();
                }
                else
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) retrieved {UserCount} users", username, role, Users.Count);
                }
                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access while fetching users", username, role);
                HttpContext.Session.Clear();
                return RedirectToPage("/Users/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading users: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi tải danh sách người dùng: {ex.Message}";
                Users = new List<UsersResponse>();
                return Page();
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete user with ID {UserId}", username, role, id);

            try
            {
                // Optional: Add check to prevent self-deletion
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                if (currentUserId.HasValue && currentUserId.Value == id)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) attempted to delete their own account with ID {UserId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Không thể xóa tài khoản của chính bạn." });
                }

                var result = await _usersService.DeleteUserAsync(id);
                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted user with ID {UserId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error deleting user with ID {UserId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting user with ID {UserId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting user with ID {UserId}: {Error}",username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting users to Excel", username, role);

            try
            {
                var users = await _usersService.GetAllUsersAsync();
                if (users == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching users for export", username, role);
                    TempData["Error"] = "Không thể tải danh sách người dùng để xuất Excel.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {UserCount} users for export", username, role, users.Count);

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Users Report");

                    string[] headers = new[]
                    {
                        "Mã Người dùng",
                        "Tên đăng nhập",
                        "Họ và Tên",
                        "Email",
                        "Vai trò",
                        "Ngày tạo"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    int row = 2;
                    foreach (var user in users)
                    {
                        worksheet.Cells[row, 1].Value = user.user_id;
                        worksheet.Cells[row, 2].Value = user.username;
                        worksheet.Cells[row, 3].Value = user.full_name;
                        worksheet.Cells[row, 4].Value = user.email;
                        worksheet.Cells[row, 5].Value = user.role;
                        worksheet.Cells[row, 6].Value = user.created_at.HasValue
                            ? user.created_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có dữ liệu";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {UserCount} users to Excel file {FileName}",username, role, users.Count, fileName);
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting users to Excel: {Error}",username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo người dùng: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}