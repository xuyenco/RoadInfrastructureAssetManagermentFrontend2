using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class TasksRequest
    {
        [Required]
        public string task_type { get; set; }
        public string work_volume { get; set; }
        [Required]
        public string status { get; set; }
        public string address { get; set; }
        [Required]
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public DateTime? start_date { get; set; }
        public DateTime? end_date { get; set; }
        public int? execution_unit_id { get; set; }
        public int? supervisor_id { get; set; }
        public string method_summary { get; set; }
        public string? main_result { get; set; }
    }
}
