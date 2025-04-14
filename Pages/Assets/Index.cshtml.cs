using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Assets
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
            // Chỉ lấy danh sách danh mục, không cần lấy asset nữa
            AssetCategories = await _assetCagetoriesService.GetAllAssetCagetoriesAsync() ?? new List<AssetCagetoriesResponse>();
        }
    }
}