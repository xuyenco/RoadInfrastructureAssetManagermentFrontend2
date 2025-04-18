using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
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
