namespace RoadInfrastructureAssetManagementFrontend2.Model.Request
{
    public class NotificationsRequest
    {
        public int user_id { get; set; }
        public int task_id { get; set; }
        public string message { get; set; }
        public bool is_read { get; set; }
        public string notification_type { get; set; }
    }
}
