using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("AccessToken");
            HttpContext.Session.Remove("RefreshToken");
            HttpContext.Session.Remove("Role"); 

            return RedirectToPage("/Users/Login");
        }
    }
}
