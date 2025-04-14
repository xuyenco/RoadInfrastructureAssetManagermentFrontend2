using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend.Interface;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUsersService _usersService;

        public IndexModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        public List<UsersResponse> Users { get; set; } = new List<UsersResponse>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Users = await _usersService.GetAllUsersAsync();
                return Page();
            }
            catch (UnauthorizedAccessException)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Users/Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading users: {ex.Message}";
                Users = new List<UsersResponse>();
                return Page();
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            Console.WriteLine($"Received user id to delete: {id}");
            try
            {
                var result = await _usersService.DeleteUserAsync(id);
                return new JsonResult(new { success = true });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try
            {
                var users = await _usersService.GetAllUsersAsync();

                ExcelPackage.License.SetNonCommercialPersonal("<Duong>");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Users Report");

                    string[] headers = new[] {
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

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xuất báo cáo người dùng: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}