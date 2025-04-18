using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IMaintenanceHistoryService
    {
        Task<List<MaintenanceHistoryResponse>> GetAllMaintenanceHistories();
        Task<List<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id);
        Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id);
        Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest request);
        Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest request);
        Task<bool> DeleteMaintenanceHistory(int id);
    }
}
