namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class MaintenanceHistoryResponse
    {
        public int maintenance_id { get; set; }
        public int asset_id { get; set; }
        public int task_id { get; set; }
        public DateTime? created_at { get; set; }
    }
}
