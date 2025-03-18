using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
