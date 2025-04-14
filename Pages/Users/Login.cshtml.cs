using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend.Interface;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class LoginModel : PageModel
    {
        private readonly IUsersService _usersService;

        public LoginModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [BindProperty]
        public LoginRequest LoginRequest { get; set; } = new LoginRequest();

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            // Hiển thị trang login
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var loginResponse = await _usersService.LoginAsync(LoginRequest);
            if (loginResponse == null)
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }

            // Kiểm tra null trước khi lưu vào Session
            if (string.IsNullOrEmpty(loginResponse.accessToken))
            {
                ErrorMessage = "Login failed: Access token is missing.";
                return Page();
            }

            // Lưu thông tin vào Session với kiểm tra null
            HttpContext.Session.SetString("AccessToken", loginResponse.accessToken);
            HttpContext.Session.SetString("RefreshToken", loginResponse.refreshToken ?? ""); // Giá trị mặc định nếu null
            HttpContext.Session.SetString("Username", loginResponse.username ?? "");
            HttpContext.Session.SetString("Role", loginResponse.role ?? "");

            // Chuyển hướng đến trang chính sau khi đăng nhập thành công
            return RedirectToPage("/Index");
        }
    }
}