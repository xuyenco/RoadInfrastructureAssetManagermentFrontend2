using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Report
{
    //[AuthorizeRole("manager")]
    public class IndexModel : PageModel
    {
        private readonly IReportService _reportService;

        public IndexModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        public List<AssetStatusReport> AssetStatusReport { get; set; } = new List<AssetStatusReport>();
        public List<IncidentDistributionReport> IncidentDistributionReport { get; set; } = new List<IncidentDistributionReport>();
        public List<TaskPerformanceReport> TaskPerformanceReport { get; set; } = new List<TaskPerformanceReport>();
        public List<IncidentTaskTrendReport> IncidentTaskTrendReport { get; set; } = new List<IncidentTaskTrendReport>();
        public List<MaintenanceFrequencyReport> MaintenanceFrequencyReport { get; set; } = new List<MaintenanceFrequencyReport>();

        public async Task OnGetAsync()
        {
            try
            {
                // Dữ liệu thực tế
                // AssetStatusReport = await _reportService.GetAssetDistributedByCondition();
                // IncidentDistributionReport = await _reportService.GetIncidentTypeDistributions();
                // TaskPerformanceReport = await _reportService.GetTaskStatusDistributions();
                // IncidentTaskTrendReport = await _reportService.GetIncidentsOverTime();
                // MaintenanceFrequencyReport = await _reportService.GetMaintenanceFrequency();

                // Dữ liệu mẫu cho AssetStatusReport
                AssetStatusReport = new List<AssetStatusReport>
                {
                    new AssetStatusReport { category_name = "Road", in_use_count = 50, damaged_count = 10 },
                    new AssetStatusReport { category_name = "Bridge", in_use_count = 30, damaged_count = 5 },
                    new AssetStatusReport { category_name = "Tunnel", in_use_count = 20, damaged_count = 8 },
                    new AssetStatusReport { category_name = "Signage", in_use_count = 100, damaged_count = 15 }
                };

                // Dữ liệu mẫu cho IncidentDistributionReport
                IncidentDistributionReport = new List<IncidentDistributionReport>
                {
                    new IncidentDistributionReport { route = "Route A", incident_count = 25 },
                    new IncidentDistributionReport { route = "Route B", incident_count = 15 },
                    new IncidentDistributionReport { route = "Route C", incident_count = 10 },
                    new IncidentDistributionReport { route = "Route D", incident_count = 5 }
                };

                // Dữ liệu mẫu cho TaskPerformanceReport
                TaskPerformanceReport = new List<TaskPerformanceReport>
                {
                    new TaskPerformanceReport { department_company_unit = "Engineering", task_count = 100, avg_hours_to_complete = 2.5 },
                    new TaskPerformanceReport { department_company_unit = "Maintenance", task_count = 80, avg_hours_to_complete = 1.8 },
                    new TaskPerformanceReport { department_company_unit = "Operations", task_count = 50, avg_hours_to_complete = 3.2 },
                    new TaskPerformanceReport { department_company_unit = "Planning", task_count = 30, avg_hours_to_complete = 2.0 }
                };

                // Dữ liệu mẫu cho IncidentTaskTrendReport
                IncidentTaskTrendReport = new List<IncidentTaskTrendReport>
                {
                    new IncidentTaskTrendReport
                    {
                        month = new DateTime(2024, 1, 1),
                        incident_count = 10,
                        task_count = 20,
                        task_status = "In Progress",
                        completed_task_count = 15
                    },
                    new IncidentTaskTrendReport
                    {
                        month = new DateTime(2024, 2, 1),
                        incident_count = 15,
                        task_count = 25,
                        task_status = "In Progress",
                        completed_task_count = 20
                    },
                    new IncidentTaskTrendReport
                    {
                        month = new DateTime(2024, 3, 1),
                        incident_count = 12,
                        task_count = 18,
                        task_status = "Completed",
                        completed_task_count = 18
                    },
                    new IncidentTaskTrendReport
                    {
                        month = new DateTime(2024, 4, 1),
                        incident_count = 8,
                        task_count = 22,
                        task_status = "In Progress",
                        completed_task_count = 10
                    },
                    new IncidentTaskTrendReport
                    {
                        month = new DateTime(2024, 5, 1),
                        incident_count = 20,
                        task_count = 30,
                        task_status = "Completed",
                        completed_task_count = 25
                    }
                };

                // Dữ liệu mẫu cho MaintenanceFrequencyReport
                MaintenanceFrequencyReport = new List<MaintenanceFrequencyReport>
                {
                    new MaintenanceFrequencyReport { asset_name = "Highway A", maintenance_count = 12 },
                    new MaintenanceFrequencyReport { asset_name = "Bridge X", maintenance_count = 8 },
                    new MaintenanceFrequencyReport { asset_name = "Tunnel Y", maintenance_count = 5 },
                    new MaintenanceFrequencyReport { asset_name = "Sign Z", maintenance_count = 15 }
                };
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"Error loading reports: {ex.Message}");
            }
        }
    }
}