using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Tasks
{
    public class TaskUpdateModel : PageModel
    {
        private readonly ITasksService _tasksService;

        public TaskUpdateModel(ITasksService tasksService)
        {
            _tasksService = tasksService;
        }

        [BindProperty]
        public TasksRequest TaskRequest { get; set; } = new TasksRequest();

        public TasksResponse TaskResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                if (TaskResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy Task với ID này.";
                    return RedirectToPage("/Tasks/Index");
                }

                // Gán giá trị mặc định cho TaskRequest từ TaskResponse
                TaskRequest = new TasksRequest
                {
                    asset_id = TaskResponse.asset_id,
                    assigned_to = TaskResponse.assigned_to,
                    task_type = TaskResponse.task_type,
                    description = TaskResponse.description,
                    priority = TaskResponse.priority,
                    status = TaskResponse.status,
                    due_date = TaskResponse.due_date
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading task: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin Task: {ex.Message}";
                return RedirectToPage("/Tasks/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
                return Page(); // Trả về trang nếu dữ liệu không hợp lệ
            }

            try
            {
                Console.WriteLine($"Updating task with ID: {id}, Asset ID: {TaskRequest.asset_id}, Assigned To: {TaskRequest.assigned_to}");

                // Gửi yêu cầu cập nhật với ID từ query string
                var updatedTask = await _tasksService.UpdateTaskAsync(id, TaskRequest);
                if (updatedTask == null)
                {
                    TempData["Error"] = "Không thể cập nhật Task. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Task update failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "Task đã được cập nhật thành công!";
                return RedirectToPage("/Tasks/Index");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật Task: {ex.Message}";
                return Page();
            }
        }
    }
}