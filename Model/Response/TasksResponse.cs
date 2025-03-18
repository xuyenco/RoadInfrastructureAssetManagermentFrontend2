namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class TasksResponse
    {
        public int task_id { get; set; }
        public int asset_id { get; set; }
        public int assigned_to { get; set; }
        public string task_type { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string priority { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime? due_date { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
