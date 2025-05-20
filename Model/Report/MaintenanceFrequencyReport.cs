namespace RoadInfrastructureAssetManagementFrontend2.Model.Report
{
    public class MaintenanceFrequencyReport
    {
        public int asset_id { get; set; }
        public string asset_name { get; set; }
        public int maintenance_count { get; set; }
        public DateTime? latest_maintenance_date { get; set; }
        public string? latest_maintenance_status { get; set; }
    }
}
