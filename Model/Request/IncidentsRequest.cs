using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class IncidentsRequest
    {
        public string address { get; set; }
        public string incident_type { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public string route { get; set; }
        public string severity_level { get; set; }
        public string damage_level { get; set; }
        public string processing_status { get; set; }
        public int? task_id { get; set; }
    }
}
