namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class MaintenanceDocumentResponse
    {
        public int document_id { get; set; }
        public int maintenance_id { get; set; }
        public string file_url { get; set; }
        public DateTime created_at { get; set; }
    }
}
