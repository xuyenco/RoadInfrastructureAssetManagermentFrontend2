using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    [AuthorizeRole("admin,manager")]
    public class UserImportModel : PageModel
    {
        private readonly IUsersService _usersService;
        private readonly ILogger<UserImportModel> _logger;

        public UserImportModel(IUsersService usersService, ILogger<UserImportModel> logger)
        {
            _usersService = usersService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the user import page", username, role);
            return Page();
        }

        public IActionResult OnGetDownloadExcelTemplateAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is downloading Excel template for user import", username, role);

            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for User input");
                var header = new List<string> { "Username", "Password Hash", "Full Name", "Email", "Role (admin, manager, technician, inspector)", "Image Path" };
                for (int i = 0; i < header.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = header[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                _logger.LogInformation("User {Username} (Role: {Role}) successfully generated Excel template for user import", username, role);
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserTemplate.xlsx");
            }
        }

        public async Task<IActionResult> OnPostAsync(IFormFile excelFile, IFormFileCollection imageFiles)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is importing users from Excel file", username, role);

            if (excelFile == null || excelFile.Length == 0)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not upload an Excel file", username, role);
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return Page();
            }

            try
            {
                var users = new List<UsersRequest>();
                var errorRows = new List<ExcelErrorRow>();
                var imageFileMap = imageFiles.ToDictionary(f => f.FileName, f => f);

                _logger.LogDebug("User {Username} (Role: {Role}) uploaded Excel file: {FileName}, size: {FileSize}", username, role, excelFile.FileName, excelFile.Length);
                _logger.LogDebug("User {Username} (Role: {Role}) uploaded {ImageCount} image files", username, role, imageFiles.Count);

                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0;
                    ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        var colCount = worksheet.Dimension?.Columns ?? 0;

                        if (rowCount < 2 || colCount < 1)
                        {
                            _logger.LogWarning("User {Username} (Role: {Role}) uploaded an empty or invalid Excel file", username, role);
                            TempData["Error"] = "File Excel trống hoặc không hợp lệ.";
                            return Page();
                        }

                        var headers = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Text?.ToLower() ?? "");
                        }

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var user = new UsersRequest();
                            string imagePath = null;
                            var rowData = new Dictionary<string, string>();

                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;
                                rowData[header] = value;

                                switch (header)
                                {
                                    case "username": user.username = value; break;
                                    case "password hash": user.password = value; break;
                                    case "full name": user.full_name = value; break;
                                    case "email": user.email = value; break;
                                    case "role (admin, manager, technician, inspector)": user.role = value; break;
                                    case "image path":
                                        imagePath = value;
                                        if (!string.IsNullOrEmpty(value) && imageFileMap.ContainsKey(Path.GetFileName(value)))
                                        {
                                            user.image = imageFileMap[Path.GetFileName(value)];
                                        }
                                        break;
                                }
                            }

                            _logger.LogDebug("User {Username} (Role: {Role}) parsed row {Row} data: {RowData}", username, role, row, JsonSerializer.Serialize(rowData));

                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(user.username))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = "Tên đăng nhập là bắt buộc."
                                });
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(user.password))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = "Mật khẩu là bắt buộc."
                                });
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(user.full_name))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = "Họ và tên là bắt buộc."
                                });
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(user.email) || !IsValidEmail(user.email))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = "Email không hợp lệ."
                                });
                                continue;
                            }

                            if (!string.IsNullOrEmpty(imagePath) && !imageFileMap.ContainsKey(Path.GetFileName(imagePath)))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) could not find image '{ImagePath}' for row {Row}", username, role, imagePath, row);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(rowData),
                                    ErrorMessage = $"Không tìm thấy ảnh '{imagePath}' trong số ảnh tải lên."
                                });
                                continue;
                            }

                            users.Add(user);
                        }

                        int successCount = 0;
                        for (int i = 0; i < users.Count; i++)
                        {
                            var user = users[i];
                            var rowNumber = i + 2;

                            // Validate role
                            if (string.IsNullOrWhiteSpace(user.role) ||
                                !new[] { "admin", "manager", "technician", "inspector" }.Contains(user.role.ToLower()))
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid role '{Role}' for row {Row}", username, role, user.role, rowNumber);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(user),
                                    ErrorMessage = "Vai trò không hợp lệ: phải là 'admin', 'manager', 'technician', hoặc 'inspector'."
                                });
                                continue;
                            }

                            try
                            {
                                _logger.LogDebug("User {Username} (Role: {Role}) creating user for row {Row}: {UserData}", username, role, rowNumber, JsonSerializer.Serialize(user));
                                var createdUser = await _usersService.CreateUserAsync(user);
                                if (createdUser == null)
                                {
                                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create user for row {Row}: No result returned", username, role, rowNumber);
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(user),
                                        ErrorMessage = "Không thể tạo người dùng (lỗi từ service)."
                                    });
                                }
                                else
                                {
                                    successCount++;
                                    _logger.LogInformation("User {Username} (Role: {Role}) successfully created user ID {UserId} for row {Row}", username, role, createdUser.user_id, rowNumber);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("User {Username} (Role: {Role}) encountered error creating user for row {Row}: {Error}", username, role, rowNumber, ex.Message);
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(user),
                                    ErrorMessage = $"Lỗi khi tạo người dùng: {ex.Message}"
                                });
                            }
                        }

                        if (errorRows.Any())
                        {
                            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
                            using (var errorPackage = new ExcelPackage())
                            {
                                var errorWorksheet = errorPackage.Workbook.Worksheets.Add("Error Rows");
                                errorWorksheet.Cells[1, 1].Value = "Row Number";
                                errorWorksheet.Cells[1, 2].Value = "Original Data";
                                errorWorksheet.Cells[1, 3].Value = "Error Message";

                                for (int i = 0; i < errorRows.Count; i++)
                                {
                                    errorWorksheet.Cells[i + 2, 1].Value = errorRows[i].RowNumber;
                                    errorWorksheet.Cells[i + 2, 2].Value = errorRows[i].OriginalData;
                                    errorWorksheet.Cells[i + 2, 3].Value = errorRows[i].ErrorMessage;
                                }
                                errorWorksheet.Cells.AutoFitColumns();

                                var errorStream = new MemoryStream(errorPackage.GetAsByteArray());
                                TempData["SuccessCount"] = successCount;
                                TempData["ErrorFile"] = Convert.ToBase64String(errorStream.ToArray());
                                TempData["Message"] = "Có lỗi trong quá trình nhập Excel. Vui lòng kiểm tra file lỗi.";
                                _logger.LogInformation("User {Username} (Role: {Role}) imported {SuccessCount} users with {ErrorCount} errors", username, role, successCount, errorRows.Count);
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                            TempData["Message"] = "Nhập Excel thành công!";
                            _logger.LogInformation("User {Username} (Role: {Role}) successfully imported {SuccessCount} users with no errors", username, role, successCount);
                        }
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error processing Excel file: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return Page();
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}