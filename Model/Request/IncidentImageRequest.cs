namespace RoadInfrastructureAssetManagementFrontend.Model.Request
{
    public class IncidentImageRequest
    {
        public int incident_id { get; set; }
        public IFormFile image { get; set; }
    }
}
