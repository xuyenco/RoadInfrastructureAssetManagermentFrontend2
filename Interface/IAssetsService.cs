using Road_Infrastructure_Asset_Management.Model.Response;
using Road_Infrastructure_Asset_Management.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
