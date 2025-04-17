using Road_Infrastructure_Asset_Management.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class IncidentsRequest
    {
        public string address { get; set; }
        [Required]
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public string route { get; set; }
        [Required]
        public string severity_level { get; set; }
        [Required]
        public string damage_level { get; set; }
        [Required]
        public string processing_status { get; set; }
        public int? task_id { get; set; }
    }
}
