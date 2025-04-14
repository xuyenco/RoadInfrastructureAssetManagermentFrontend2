using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
