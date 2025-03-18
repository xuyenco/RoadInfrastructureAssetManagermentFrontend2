using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Assets
{
    public class IndexModel : PageModel
    {
        private readonly IAssetsService _assetsService;
        public IndexModel(IAssetsService assetsService)
        {
            _assetsService = assetsService;
        }
        public List<AssetsResponse> Assets { get; set; } = new();

        public async Task OnGetAsync()
        {
            Assets = await _assetsService.GetAllAssetsAsync();
        }
    }
}
