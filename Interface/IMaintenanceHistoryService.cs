using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using static RoadInfrastructureAssetManagementFrontend2.Service.MaintenanceHistoryService;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IMaintenanceHistoryService
    {
        Task<List<MaintenanceHistoryResponse>> GetAllMaintenanceHistories();
        Task<List<MaintenanceHistoryResponse>> GetMaintenanceHistoryByAssetId(int id);
        Task<MaintenanceHistoryResponse?> GetMaintenanceHistoryById(int id);
        Task<PagedMaintenanceHistoryResult> GetPagedMaintenanceHistoryByAssetId(int id, int currentPage, int pageSize, string searchTerm, int searchField);
        Task<MaintenanceHistoryResponse?> CreateMaintenanceHistory(MaintenanceHistoryRequest request);
        Task<MaintenanceHistoryResponse?> UpdateMaintenanceHistory(int id, MaintenanceHistoryRequest request);
        Task<bool> DeleteMaintenanceHistory(int id);
    }
}
