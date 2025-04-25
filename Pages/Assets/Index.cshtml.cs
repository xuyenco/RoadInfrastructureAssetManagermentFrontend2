using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Interface;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class IndexModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAssetCagetoriesService assetCagetoriesService, ILogger<IndexModel> logger)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing Asset Index page", username, role); // Log truy cập trang
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();

        }
    }
}