namespace RoadInfrastructureAssetManagementFrontend2.Model.Report
{
    public class IncidentTaskTrendReport
    {
        public DateTime month { get; set; }
        public int incident_count { get; set; }
        public int task_count { get; set; }
        public string task_status { get; set; }
        public int completed_task_count { get; set; }
    }
}
