using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using RoadInfrastructureAssetManagementFrontend.Interface;

namespace RoadInfrastructureAssetManagementFrontend.Pages.Incidents
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentsService _incidentsService;

        public IEnumerable<IncidentsResponse> Incidents { get; private set; } = new List<IncidentsResponse>();
        public int IncidentCount => Incidents.Count();

        public IndexModel(IIncidentsService incidentsService)
        {
            _incidentsService = incidentsService;
        }

        public async Task OnGetAsync()
        {
            Incidents = await _incidentsService.GetAllIncidentsAsync();
        }
    }
}
