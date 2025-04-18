namespace RoadInfrastructureAssetManagementFrontend2.Model.Request
{
    public class MaintenanceDocumentRequest
    {
        public int maintenance_id { get; set; }
        public IFormFile file { get; set; }
    }
}
