using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("AccessToken");
            HttpContext.Session.Remove("RefreshToken");
            HttpContext.Session.Remove("Role");
            HttpContext.Session.Remove("Id");

            return RedirectToPage("/index");
        }
    }
}
