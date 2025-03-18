using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
