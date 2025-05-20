using RoadInfrastructureAssetManagementFrontend2.Model.Report;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IReportService
    {
        Task<List<AssetStatusReport>> GetAssetDistributedByCondition();
        Task<List<IncidentDistributionReport>> GetIncidentTypeDistributions();
        Task<List<TaskPerformanceReport>> GetTaskStatusDistributions();
        Task<List<IncidentTaskTrendReport>> GetIncidentsOverTime();
        Task<List<MaintenanceFrequencyReport>> GetMaintenanceFrequency();
    }
}
