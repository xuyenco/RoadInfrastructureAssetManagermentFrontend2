using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Geometry;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;
using System;
using System.Text.Json;
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

        [BindProperty]
        public TasksResponse TaskResponse { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                if (TaskResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy nhiệm vụ với ID này.";
                    return RedirectToPage("/Tasks/Index");
                }
                TaskResponse.geometry = CoordinateConverter.ConvertGeometryToWGS84(TaskResponse.geometry);

                TaskRequest = new TasksRequest
                {
                    task_type = TaskResponse.task_type,
                    work_volume = TaskResponse.work_volume,
                    status = TaskResponse.status,
                    address = TaskResponse.address,
                    geometry = TaskResponse.geometry,
                    start_date = TaskResponse.start_date,
                    end_date = TaskResponse.end_date,
                    execution_unit_id = TaskResponse.execution_unit_id,
                    supervisor_id = TaskResponse.supervisor_id,
                    method_summary = TaskResponse.method_summary,
                    main_result = TaskResponse.main_result
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading task: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin nhiệm vụ: {ex.Message}";
                return RedirectToPage("/Tasks/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (TaskRequest.geometry == null) TaskRequest.geometry = new GeoJsonGeometry();

            var geometryType = Request.Form["TaskRequest.geometry.type"];
            if (!string.IsNullOrEmpty(geometryType) && (geometryType == "Point" || geometryType == "LineString"))
            {
                TaskRequest.geometry.type = geometryType;
                ModelState["TaskRequest.geometry.type"].Errors.Clear();
                ModelState["TaskRequest.geometry.type"].ValidationState = ModelValidationState.Valid;
            }
            else
            {
                ModelState.AddModelError("TaskRequest.geometry.type", "Loại hình học phải là 'Point' hoặc 'LineString'.");
            }

            var coordinatesJson = Request.Form["TaskRequest.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    TaskRequest.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    Console.WriteLine($"Raw coordinates: {JsonSerializer.Serialize(TaskRequest.geometry.coordinates)}");

                    if (GeometrySystem == "wgs84")
                    {
                        TaskRequest.geometry = CoordinateConverter.ConvertGeometryToVN2000(TaskRequest.geometry);
                        Console.WriteLine($"Converted to VN2000: {JsonSerializer.Serialize(TaskRequest.geometry.coordinates)}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("TaskRequest.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("TaskRequest.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            //if (!ModelState.IsValid)
            //{
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine($"Validation error: {error.ErrorMessage}");
            //    }
            //    TaskResponse = await _tasksService.GetTaskByIdAsync(id);
            //    return Page();
            //}

            Console.WriteLine($"Data send to backend: {JsonSerializer.Serialize(TaskRequest)}");

            try
            {
                Console.WriteLine($"Updating task with ID: {id}, Task Type: {TaskRequest.task_type}");
                var updatedTask = await _tasksService.UpdateTaskAsync(id, TaskRequest);
                if (updatedTask == null)
                {
                    TempData["Error"] = "Không thể cập nhật nhiệm vụ. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("Task update failed: null response from service.");
                    TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                    return Page();
                }

                TempData["Success"] = "Nhiệm vụ đã được cập nhật thành công!";
                return RedirectToPage("/Tasks/Index");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Argument error: {ex.Message}");
                TempData["Error"] = ex.Message;
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Invalid operation: {ex.Message}");
                TempData["Error"] = ex.Message;
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật nhiệm vụ: {ex.Message}";
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
        }
    }
}