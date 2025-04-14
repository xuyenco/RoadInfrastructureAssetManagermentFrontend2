using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Model.Report;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Service
{
    public class ReportService :BaseService,IReportService
    {
        public ReportService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, httpContextAccessor)
        {
        }

        public async Task<List<TaskStatusDistribution>> GetTaskStatusDistributions()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/TaskStatusDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve task status distribution report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<TaskStatusDistribution>>(content);
        }

        public async Task<List<IncidentTypeDistribution>> GetIncidentTypeDistributions()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/IncidentTypeDistribution"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve Incident distribution by type report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentTypeDistribution>>(content);
        }
        public async Task<List<IncidentsOverTime>> GetIncidentsOverTime()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/IncidentsOverTime"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve Incident over time report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<IncidentsOverTime>>(content);
        }
        public async Task<List<BudgetAndCost>> GetBudgetAndCosts()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/BudgetAndCosts"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve Budget and Costs report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<BudgetAndCost>>(content);
        }
        public async Task<List<AssetDistributionByCategory>> GetAssetDistributionByCategories()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/AssetDistributionByCategories"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve Asset distribution by categories report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AssetDistributionByCategory>>(content);
        }
        public async Task<List<AssetDistributedByCondition>> GetAssetDistributedByCondition()
        {
            var response = await ExecuteWithRefreshAsync(()=> _httpClient.GetAsync("api/Report/AssetDistributedByCondition"));
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to retrieve Asset distribution by condition report: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AssetDistributedByCondition>>(content);
        }
    }
}
