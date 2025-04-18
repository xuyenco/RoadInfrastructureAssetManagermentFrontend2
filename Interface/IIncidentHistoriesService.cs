using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IIncidentHistoriesService
    {
        Task<List<IncidentHistoriesResponse>> GetAllIncidentHistoriesAsync();
        Task<IncidentHistoriesResponse?> GetIncidentHistoryByIdAsync(int id);
        Task<List<IncidentHistoriesResponse>> GetIncidentHistoriesByIncidentId(int id);
        Task<IncidentHistoriesResponse?> CreateIncidentHistoryAsync(IncidentHistoriesRequest request);
        Task<IncidentHistoriesResponse?> UpdateIncidentHistoryAsync(int id, IncidentHistoriesRequest request);
        Task<bool> DeleteIncidentHistoryAsync(int id);
    }
}
