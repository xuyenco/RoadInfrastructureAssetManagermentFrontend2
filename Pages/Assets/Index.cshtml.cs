using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Interface;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Assets
{
    public class IndexModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public IndexModel(IAssetCagetoriesService assetCagetoriesService)
        {
            _assetCagetoriesService = assetCagetoriesService;
        }

        public List<AssetCagetoriesResponse> AssetCategories { get; set; } = new();

        public async Task OnGetAsync()
        {
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
        }
    }
}