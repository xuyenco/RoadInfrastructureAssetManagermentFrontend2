using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public IndexModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        public List<TasksResponse> Tasks { get; set; } = new List<TasksResponse>(); 

        public async Task OnGetAsync()
        {
            Tasks = await _tasksService.GetAllTasksAsync(); // Lấy danh sách nhiệm vụ
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _tasksService.DeleteTaskAsync(id);
            if (result)
            {
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false, message = "Không thể xóa nhiệm vụ." });
        }
    }
}
