using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Model.Response;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class UserImportModel : PageModel
    {
        private readonly IUsersService _usersService;

        public UserImportModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        public void OnGet()
        {
            // Trang trống khi tải lần đầu
        }

        public async Task<IActionResult> OnGetDownloadExcelTemplateAsync()
        {
            ExcelPackage.License.SetNonCommercialPersonal("<Duong>");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Template for User input");
                var header = new List<string> { "Username", "Password Hash", "Full Name", "Email", "Role (admin, manager, technician, inspector)", "Image Path" };
                for (int i = 0; i < header.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = header[i];
                }
                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserTemplate.xlsx");
            }
        }

        public async Task<IActionResult> OnPostAsync(IFormFile excelFile, IFormFileCollection imageFiles)
        {
            Console.WriteLine("OnPostAsync for UserImport started!");
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return Page();
            }

            try
            {
                var users = new List<UsersRequest>();
                var errorRows = new List<ExcelErrorRow>();
                var imageFileMap = imageFiles.ToDictionary(f => f.FileName, f => f);

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

                            for (int col = 1; col <= colCount; col++)
                            {
                                var header = headers[col - 1];
                                var value = worksheet.Cells[row, col].Text;

                                switch (header)
                                {
                                    case "username": user.username = value; break;
                                    case "password hash": user.password_hash = value; break;
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

                            if (!string.IsNullOrEmpty(imagePath) && !imageFileMap.ContainsKey(Path.GetFileName(imagePath)))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = row,
                                    OriginalData = JsonSerializer.Serialize(user),
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

                            if (!new[] { "admin", "manager", "technician", "inspector" }.Contains(user.role))
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(user),
                                    ErrorMessage = "Role không hợp lệ."
                                });
                                Console.WriteLine($"User Role: {user.role}");
                                continue;
                            }

                            try
                            {
                                var createUser = await _usersService.CreateUserAsync(user);
                                if (createUser == null)
                                {
                                    errorRows.Add(new ExcelErrorRow
                                    {
                                        RowNumber = rowNumber,
                                        OriginalData = JsonSerializer.Serialize(user),
                                        ErrorMessage = "Không thể tạo user (lỗi từ service)."
                                    });
                                }
                                else
                                {
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorRows.Add(new ExcelErrorRow
                                {
                                    RowNumber = rowNumber,
                                    OriginalData = JsonSerializer.Serialize(user),
                                    ErrorMessage = $"Lỗi khi tạo user: {ex.Message}"
                                });
                            }
                        }

                        if (errorRows.Any())
                        {
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
                            }
                        }
                        else
                        {
                            TempData["SuccessCount"] = successCount;
                            TempData["Message"] = "Nhập Excel thành công!";
                        }
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xử lý file Excel: {ex.Message}";
                return Page();
            }
        }
    }
}