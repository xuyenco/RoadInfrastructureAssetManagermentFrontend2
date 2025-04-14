using Road_Infrastructure_Asset_Management.Model.ImageUpload;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
