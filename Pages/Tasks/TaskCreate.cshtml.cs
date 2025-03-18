using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Tasks
{
    public class TaskCreateModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public TaskCreateModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        [BindProperty]
        public TasksRequest Task { get; set; } = new TasksRequest();

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

            var createdTask = await _tasksService.CreateTaskAsync(Task);
            if (createdTask == null)
            {
                ModelState.AddModelError("", "Không thể tạo Task. Vui lòng kiểm tra lại dữ liệu.");
                return Page();
            }

            return RedirectToPage("/Tasks/Index"); // Chuyển hướng về trang Index sau khi tạo thành công
        }
    }
}
