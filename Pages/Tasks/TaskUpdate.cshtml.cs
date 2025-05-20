using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Tasks
{
    //[AuthorizeRole("inspector,technician")]
    public class TaskUpdateModel : PageModel
    {
        private readonly ITasksService _tasksService;
        private readonly ILogger<TaskUpdateModel> _logger;

        public TaskUpdateModel(ITasksService tasksService, ILogger<TaskUpdateModel> logger)
        {
            _tasksService = tasksService;
            _logger = logger;
        }

        [BindProperty]
        public TasksRequest TaskRequest { get; set; } = new TasksRequest();

        [BindProperty]
        public TasksResponse TaskResponse { get; set; }

        [BindProperty]
        public string GeometrySystem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing update page for task with ID {TaskId}",
                username, role, id);

            try
            {
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                if (TaskResponse == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) found no task with ID {TaskId}",
                        username, role, id);
                    TempData["Error"] = "Không tìm thấy nhiệm vụ với ID này.";
                    return RedirectToPage("/Tasks/Index");
                }

                TaskResponse.geometry = CoordinateConverter.ConvertGeometryToWGS84(TaskResponse.geometry);
                _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to WGS84 for task ID {TaskId}: {Geometry}",
                    username, role, id, JsonSerializer.Serialize(TaskResponse.geometry));

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
                    main_result = TaskResponse.main_result,
                    description = TaskResponse.description,
                };

                _logger.LogInformation("User {Username} (Role: {Role}) successfully loaded task with ID {TaskId} for update",
                    username, role, id);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error loading task with ID {TaskId}: {Error}",
                    username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin nhiệm vụ: {ex.Message}";
                return RedirectToPage("/Tasks/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting update for task with ID {TaskId}", username, role, id);
            _logger.LogDebug("User {Username} (Role: {Role}) submitted form data: {FormData}", username, role, JsonSerializer.Serialize(Request.Form));

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
                _logger.LogWarning("User {Username} (Role: {Role}) provided invalid geometry type for task ID {TaskId}: {GeometryType}", username, role, id, geometryType);
                ModelState.AddModelError("TaskRequest.geometry.type", "Loại hình học phải là 'Point' hoặc 'LineString'.");
            }

            var coordinatesJson = Request.Form["TaskRequest.geometry.coordinates"];
            if (!string.IsNullOrEmpty(coordinatesJson))
            {
                try
                {
                    TaskRequest.geometry.coordinates = JsonSerializer.Deserialize<object>(coordinatesJson);
                    _logger.LogDebug("User {Username} (Role: {Role}) deserialized coordinates for task ID {TaskId}: {Coordinates}", username, role, id, JsonSerializer.Serialize(TaskRequest.geometry.coordinates));

                    if (GeometrySystem == "wgs84")
                    {
                        TaskRequest.geometry = CoordinateConverter.ConvertGeometryToVN2000(TaskRequest.geometry);
                        _logger.LogDebug("User {Username} (Role: {Role}) converted geometry to VN2000 for task ID {TaskId}: {Geometry}", username, role, id, JsonSerializer.Serialize(TaskRequest.geometry));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) provided invalid coordinates for task ID {TaskId}: {Error}", username, role, id, ex.Message);
                    ModelState.AddModelError("TaskRequest.geometry.coordinates", $"Định dạng tọa độ không hợp lệ: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide geometry coordinates for task ID {TaskId}", username, role, id);
                ModelState.AddModelError("TaskRequest.geometry.coordinates", "Tọa độ là bắt buộc.");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(TaskRequest.task_type))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide task_type for task ID {TaskId}", username, role, id);
                ModelState.AddModelError("TaskRequest.task_type", "Loại nhiệm vụ là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(TaskRequest.status))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide status for task ID {TaskId}", username, role, id);
                ModelState.AddModelError("TaskRequest.status", "Trạng thái là bắt buộc.");
            }

            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.ToDictionary(
            //        kvp => kvp.Key,
            //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            //    );
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors for task ID {TaskId}: {Errors}",username, role, id, JsonSerializer.Serialize(errors));
            //    TaskResponse = await _tasksService.GetTaskByIdAsync(id);
            //    return Page();
            //}

            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) updating task with ID {TaskId}: {TaskData}", username, role, id, JsonSerializer.Serialize(TaskRequest));
                var updatedTask = await _tasksService.UpdateTaskAsync(id, TaskRequest);
                if (updatedTask == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to update task with ID {TaskId}: No result returned", username, role, id);
                    TempData["Error"] = "Không thể cập nhật nhiệm vụ. Dữ liệu trả về từ dịch vụ là null.";
                    TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                    return Page();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully updated task with ID {TaskId}", username, role, id);
                TempData["Success"] = "Nhiệm vụ đã được cập nhật thành công!";
                return RedirectToPage("/Tasks/TasksTable");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error updating task with ID {TaskId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = ex.Message;
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error updating task with ID {TaskId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = ex.Message;
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error updating task with ID {TaskId}: {Error}", username, role, id, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật nhiệm vụ: {ex.Message}";
                TaskResponse = await _tasksService.GetTaskByIdAsync(id);
                return Page();
            }
        }
    }
}