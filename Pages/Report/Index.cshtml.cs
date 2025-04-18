using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Report;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Report
{
    public class IndexModel : PageModel
    {
        private readonly IReportService _reportService;

        public IndexModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        public List<AssetDistributionByCategory> AssetDistribution { get; set; } = new List<AssetDistributionByCategory>() ;
        public List<AssetDistributedByCondition> AssetCondition { get; set; } = new List<AssetDistributedByCondition>();
        public List<TaskStatusDistribution> TaskStatus { get; set; } = new List<TaskStatusDistribution>();
        public List<IncidentTypeDistribution> IncidentTypes { get; set; } = new List<IncidentTypeDistribution>();
        public List<IncidentsOverTime> IncidentsOverTime { get; set; } = new List<IncidentsOverTime>();
        public List<BudgetAndCost> BudgetAndCosts { get; set; } = new List<BudgetAndCost>();

        public async Task OnGetAsync()
        {
            //try
            //{
            //    AssetDistribution = await _reportService.GetAssetDistributionByCategories();
            //    AssetCondition = await _reportService.GetAssetDistributedByCondition();
            //    TaskStatus = await _reportService.GetTaskStatusDistributions();
            //    IncidentTypes = await _reportService.GetIncidentTypeDistributions();
            //    IncidentsOverTime = await _reportService.GetIncidentsOverTime();
            //    BudgetAndCosts = await _reportService.GetBudgetAndCosts();
            //}
            //catch (HttpRequestException ex)
            //{
            //    ModelState.AddModelError("", $"Error loading reports: {ex.Message}");

            //}



            //Dữ liệu mẫu cho TaskStatusDistribution(của bạn)
            TaskStatus.Add(new TaskStatusDistribution { status = "pending", count = 25 });
            TaskStatus.Add(new TaskStatusDistribution { status = "in-progress", count = 15 });
            TaskStatus.Add(new TaskStatusDistribution { status = "completed", count = 35 });
            TaskStatus.Add(new TaskStatusDistribution { status = "cancelled", count = 5 });

            //Dữ liệu mẫu cho AssetDistributionByCategory
            AssetDistribution.Add(new AssetDistributionByCategory { category_name = "Traffic Lights", count = 50 });
            AssetDistribution.Add(new AssetDistributionByCategory { category_name = "Roads", count = 30 });
            AssetDistribution.Add(new AssetDistributionByCategory { category_name = "Bridges", count = 20 });

            //Dữ liệu mẫu cho AssetDistributedByCondition
            AssetCondition.Add(new AssetDistributedByCondition { condition = "Tốt", count = 60 });
            AssetCondition.Add(new AssetDistributedByCondition { condition = "Trung bình", count = 30 });
            AssetCondition.Add(new AssetDistributedByCondition { condition = "Kém", count = 10 });

            //Dữ liệu mẫu cho IncidentTypeDistribution
            IncidentTypes.Add(new IncidentTypeDistribution { incident_type = "Pothole", count = 15 });
            IncidentTypes.Add(new IncidentTypeDistribution { incident_type = "Accident", count = 10 });
            IncidentTypes.Add(new IncidentTypeDistribution { incident_type = "Flooding", count = 5 });

            //Dữ liệu mẫu cho IncidentsOverTime
            IncidentsOverTime.Add(new IncidentsOverTime { year = 2023, month = 1, count = 5 });
            IncidentsOverTime.Add(new IncidentsOverTime { year = 2023, month = 2, count = 8 });
            IncidentsOverTime.Add(new IncidentsOverTime { year = 2023, month = 3, count = 3 });
            IncidentsOverTime.Add(new IncidentsOverTime { year = 2023, month = 4, count = 6 });

            //Dữ liệu mẫu cho BudgetAndCost
            BudgetAndCosts.Add(new BudgetAndCost { fiscal_year = 2021, total_cost = 0, total_budget = 15000000 });
            BudgetAndCosts.Add(new BudgetAndCost { fiscal_year = 2022, total_cost = 2800000, total_budget = 20000000 });
            BudgetAndCosts.Add(new BudgetAndCost { fiscal_year = 2023, total_cost = 5000000, total_budget = 25000000 });
        }
    }
}