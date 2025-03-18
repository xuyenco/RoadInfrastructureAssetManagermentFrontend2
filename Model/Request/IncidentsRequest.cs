using Road_Infrastructure_Asset_Management.Model.Geometry;

namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class IncidentsRequest
    {
        public int asset_id { get; set; } // optional
        public int reported_by { get; set; }
        public string incident_type { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public GeoJsonGeometry location { get; set; } = new GeoJsonGeometry();
        public string priority { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime? resolved_at { get; set; }
        public string notes { get; set; } = string.Empty;

    }
}
