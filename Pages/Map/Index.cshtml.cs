using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Map
{
    public class IndexModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly IAssetsService _assetsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAssetCagetoriesService assetCagetoriesService, ILogger<IndexModel> logger, IAssetsService assetsService)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
            _assetsService = assetsService;
        }
        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task OnGetAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing Asset Index page", username, role);
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
        }
    }
}
