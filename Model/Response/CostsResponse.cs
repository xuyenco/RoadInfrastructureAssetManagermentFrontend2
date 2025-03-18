namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class CostsResponse
    {
        public int cost_id { get; set; }
        public int task_id { get; set; }
        public string cost_type { get; set; } = string.Empty;
        public double amount { get; set; }
        public string description { get; set; } = string.Empty;
        public DateTime? date_incurred { get; set; }
        public DateTime? created_at { get; set; }
    }
}
