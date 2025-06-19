using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Service;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    public class UserDetailModel : PageModel
    {
        private readonly IUsersService _usersService;

        public UserDetailModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        public UsersResponse? User { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue)
            {
                TempData["Error"] = "ID người dùng không hợp lệ.";
                return Page();
            }

            try
            {
                User = await _usersService.GetUserByIdAsync(id.Value);
                if (User == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng với ID này.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage("/Users/Login");
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] = $"Lỗi khi tải thông tin người dùng: {ex.Message}";
            }

            return Page();
        }
    }
}