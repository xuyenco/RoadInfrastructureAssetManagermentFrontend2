using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeOpenXml;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Drawing;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class AssetsTableModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        private readonly ILogger<AssetsTableModel> _logger;

        public AssetsTableModel(IAssetsService assetsService, ILogger<AssetsTableModel> logger)
        {
            _assetsService = assetsService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the assets table page", username, role);
            return Page();
        }

        public async Task<IActionResult> OnGetAssetsAsync(int currentPage = 1, int pageSize = 10, string searchTerm = "", int searchField = 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is fetching assets - Page: {CurrentPage}, PageSize: {PageSize}, SearchTerm: {SearchTerm}, SearchField: {SearchField}",
                username, role, currentPage, pageSize, searchTerm, searchField);

            try
            {
                var (assets, totalCount) = await _assetsService.GetAssetsAsync(currentPage, pageSize, searchTerm, searchField);
                if (assets == null || !assets.Any())
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received empty assets list for Page: {CurrentPage}", username, role, currentPage);
                    return new JsonResult(new { success = true, assets = new List<AssetsResponse>(), totalCount = 0 });
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets with total count {TotalCount} for Page: {CurrentPage}",
                    username, role, assets.Count, totalCount, currentPage);
                return new JsonResult(new { success = true, assets, totalCount });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered unauthorized access while fetching assets", username, role);
                HttpContext.Session.Clear();
                return new JsonResult(new { success = false, message = "Phiên làm việc hết hạn, vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error fetching assets: {Error}", username, role, ex.Message);
                return new JsonResult(new { success = false, message = $"Lỗi khi tải danh sách tài sản: {ex.Message}" });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is attempting to delete asset with ID {AssetId}", username, role, id);

            try
            {
                var deleted = await _assetsService.DeleteAssetAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to delete asset with ID {AssetId}", username, role, id);
                    return new JsonResult(new { success = false, message = "Xóa tài sản thất bại." });
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully deleted asset with ID {AssetId}", username, role, id);
                return new JsonResult(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error deleting asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error deleting asset with ID {AssetId}: {Error}", username, role, id, ex.Message);
                return new JsonResult(new { success = false, message = $"Đã xảy ra lỗi khi xóa tài sản: {ex.Message}" });
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is exporting assets to Excel", username, role);

            try
            {
                var assets = await _assetsService.GetAllAssetsAsync();
                if (assets == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) received null response when fetching assets for export", username, role);
                    TempData["Error"] = "Không thể tải danh sách tài sản để xuất Excel.";
                    return RedirectToPage();
                }
                _logger.LogInformation("User {Username} (Role: {Role}) retrieved {AssetCount} assets for export", username, role, assets.Count);

                ExcelPackage.License.SetNonCommercialPersonal("Duong");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Assets Report");

                    string[] headers = new[]
                    {
                        "Mã tài sản",
                        "Tên tài sản",
                        "Mã số tài sản",
                        "Địa chỉ",
                        "Trạng thái",
                        "Ngày tạo",
                        "Hình ảnh"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    int row = 2;
                    foreach (var asset in assets)
                    {
                        worksheet.Cells[row, 1].Value = asset.asset_id;
                        worksheet.Cells[row, 2].Value = asset.asset_name;
                        worksheet.Cells[row, 3].Value = asset.asset_code;
                        worksheet.Cells[row, 4].Value = asset.address;
                        worksheet.Cells[row, 5].Value = asset.asset_status;
                        worksheet.Cells[row, 6].Value = asset.created_at.HasValue
                            ? asset.created_at.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa có dữ liệu";
                        worksheet.Cells[row, 7].Value = asset.image_url ?? "Không có hình ảnh";
                        row++;
                    }

                    worksheet.Cells.AutoFitColumns();

                    var stream = new MemoryStream(package.GetAsByteArray());
                    string fileName = $"Assets_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    _logger.LogInformation("User {Username} (Role: {Role}) successfully exported {AssetCount} assets to Excel file {FileName}", username, role, assets.Count, fileName);
                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error exporting assets to Excel: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Lỗi khi xuất báo cáo tài sản: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}