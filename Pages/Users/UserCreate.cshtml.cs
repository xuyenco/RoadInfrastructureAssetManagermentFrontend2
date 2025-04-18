using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    [AuthorizeRole("admin")]
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
            Console.WriteLine("OnPostAsync for UserCreate started!");
            User.image = Request.Form.Files["image"];
            Console.WriteLine($"Bound Data: username={User.username}, password_hash={User.password_hash}, full_name={User.full_name}, email={User.email}, role={User.role}");
            if (User.image != null)
            {
                Console.WriteLine($"Image: filename={User.image.FileName}, size={User.image.Length}");
            }

            try
            {
                var createdUser = await _usersService.CreateUserAsync(User);
                if (createdUser == null)
                {
                    TempData["Error"] = "Không thể tạo User. Dữ liệu trả về từ dịch vụ là null.";
                    return Page();
                }

                TempData["Success"] = "User đã được tạo thành công!";
                return RedirectToPage("/Users/Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo User: {ex.Message}";
                return Page();
            }
        }
    }
}