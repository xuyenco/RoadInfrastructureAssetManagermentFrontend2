using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IBudgetsService
    {
        Task<List<BudgetsResponse>> GetAllBudgetsAsync();
        Task<BudgetsResponse?> GetBudgetByIdAsync(int id);
        Task<BudgetsResponse?> CreateBudgetAsync(BudgetsRequest request);
        Task<BudgetsResponse?> UpdateBudgetAsync(int id, BudgetsRequest request);
        Task<bool> DeleteBudgetAsync(int id);
    }
}
