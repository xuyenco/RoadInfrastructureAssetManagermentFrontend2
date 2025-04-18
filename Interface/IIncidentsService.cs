using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IIncidentsService
    {
        Task<List<IncidentsResponse>> GetAllIncidentsAsync();
        Task<IncidentsResponse?> GetIncidentByIdAsync(int id);
        Task<IncidentsResponse?> CreateIncidentAsync(IncidentsRequest request);
        Task<IncidentsResponse?> UpdateIncidentAsync(int id, IncidentsRequest request);
        Task<bool> DeleteIncidentAsync(int id);
    }
}
