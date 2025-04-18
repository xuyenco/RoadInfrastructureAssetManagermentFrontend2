using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;

namespace RoadInfrastructureAssetManagementFrontend2.Model.Request
{
    public class IncidentsResponse
    {
        public int incident_id { get; set; }
        public string address { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public string route { get; set; }
        public string severity_level { get; set; }
        public string damage_level { get; set; }
        public string processing_status { get; set; }
        public int? task_id { get; set; }
        public DateTime? created_at { get; set; }
    }
}
