using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class UserCreateModel : PageModel
    {
        private readonly IUsersService _usersService;

        public UserCreateModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [BindProperty]
        public UsersRequest User { get; set; } = new UsersRequest();

        public void OnGet()
        {
            // Hiển thị form rỗng khi trang được tải
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Trả về trang nếu dữ liệu không hợp lệ
            }

            var createdUser = await _usersService.CreateUserAsync(User);
            if (createdUser == null)
            {
                ModelState.AddModelError("", "Không thể tạo User. Vui lòng kiểm tra lại dữ liệu.");
                return Page();
            }

            return RedirectToPage("/Users/Index"); // Chuyển hướng về trang Index sau khi tạo thành công
        }
    }
}
