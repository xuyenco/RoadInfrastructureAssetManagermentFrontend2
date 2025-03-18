using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
{
    public interface ITasksService
    {
        Task<List<TasksResponse>> GetAllTasksAsync();
        Task<TasksResponse?> GetTaskByIdAsync(int id);
        Task<TasksResponse?> CreateTaskAsync(TasksRequest request);
        Task<TasksResponse?> UpdateTaskAsync(int id, TasksRequest request);
        Task<bool> DeleteTaskAsync(int id);
    }
}
