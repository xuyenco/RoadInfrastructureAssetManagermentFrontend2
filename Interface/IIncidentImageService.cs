using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IIncidentImageService
    {
        Task<List<IncidentImageResponse>> GetAllIncidentImagesAsync();
        Task<IncidentImageResponse?> GetIncidentImageByIdAsync(int id);
        Task<List<IncidentImageResponse>> GetAllIncidentImagesByIncidentId(int id);
        Task<IncidentImageResponse?> CreateIncidentImageAsync(IncidentImageRequest request);
        Task<IncidentImageResponse?> UpdateIncidentImageAsync(int id, IncidentImageRequest request);
        Task<bool> DeleteIncidentImageAsync(int id);
    }
}
