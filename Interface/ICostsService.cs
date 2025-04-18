using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface ICostsService
    {
        Task<List<CostsResponse>> GetAllCostsAsync();
        Task<CostsResponse?> GetCostByIdAsync(int id);
        Task<CostsResponse?> CreateCostAsync(CostsRequest request);
        Task<CostsResponse?> UpdateCostAsync(int id, CostsRequest response);
        Task<bool> DeleteCostAsync(int id);
    }
}
