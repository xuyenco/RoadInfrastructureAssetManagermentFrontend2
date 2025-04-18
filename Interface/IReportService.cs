using RoadInfrastructureAssetManagementFrontend2.Model.Report;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IReportService
    {
        Task<List<TaskStatusDistribution>> GetTaskStatusDistributions();
        Task<List<IncidentTypeDistribution>> GetIncidentTypeDistributions();
        Task<List<IncidentsOverTime>> GetIncidentsOverTime();
        Task<List<BudgetAndCost>> GetBudgetAndCosts();
        Task<List<AssetDistributionByCategory>> GetAssetDistributionByCategories();
        Task<List<AssetDistributedByCondition>> GetAssetDistributedByCondition();
    }
}
