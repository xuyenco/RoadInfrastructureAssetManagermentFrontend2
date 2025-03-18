using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend.Interface
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
