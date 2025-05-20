namespace RoadInfrastructureAssetManagementFrontend2.Model.Report
{
    public class TaskPerformanceReport
    {
        public string department_company_unit { get; set; }
        public int task_count { get; set; }
        public double avg_hours_to_complete { get; set; }
    }
}
