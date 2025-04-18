using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IAssetCagetoriesService
    {
        Task<List<AssetCagetoriesResponse>> GetAllAssetCagetoriesAsync();
        Task<AssetCagetoriesResponse?> GetAssetCagetoriesByIdAsync(int id);
        Task<AssetCagetoriesResponse?> CreateAssetCagetoriesAsync(AssetCagetoriesRequest request);
        Task<AssetCagetoriesResponse?> UpdateAssetCagetoriesAsync(int id, AssetCagetoriesRequest request);
        Task<bool> DeleteAssetCagetoriesAsync(int id);
    }
}
