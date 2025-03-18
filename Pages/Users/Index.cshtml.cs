using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUsersService _usersService;

        public IndexModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        public List<UsersResponse> Users { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _usersService.GetAllUsersAsync();
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _usersService.DeleteUserAsync(id);
            if (result)
            {
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Không thể xóa người dùng." });
        }
    }
}
