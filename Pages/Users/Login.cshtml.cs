using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
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

            try
            {
                var result = await _usersService.LoginAsync(LoginRequest);
                if (result != null)
                {
                    HttpContext.Session.SetString("AccessToken", result.accessToken);
                    HttpContext.Session.SetString("RefreshToken", result.refreshToken);
                    HttpContext.Session.SetString("Role", result.role);
                    HttpContext.Session.SetString("Username", result.username);
                    HttpContext.Session.SetInt32("Id", result.id);

                    // Commit Session để đảm bảo cookie được gửi
                    await HttpContext.Session.CommitAsync();

                    return RedirectToPage("/index");
                }
                ModelState.AddModelError("", "Invalid login attempt.");
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return Page();
            }
        }
    }
}