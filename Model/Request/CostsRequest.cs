namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class CostsRequest
    {
        public int task_id { get; set; }
        public string cost_type { get; set; } = string.Empty;
        public double amount { get; set; }
        public string description { get; set; } = string.Empty;
        public DateTime? date_incurred { get; set; }
    }
}
