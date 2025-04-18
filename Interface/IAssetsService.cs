using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IAssetsService
    {
        Task<List<AssetsResponse>> GetAllAssetsAsync();
        Task<AssetsResponse?> GetAssetByIdAsync(int id);
        Task<AssetsResponse?> CreateAssetAsync(AssetsRequest request);
        Task<AssetsResponse?> UpdateAssetAsync(int id, AssetsRequest request);
        Task<bool> DeleteAssetAsync(int id);
    }
}
