using Road_Infrastructure_Asset_Management.Model.Geometry;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetsResponse
    {
        public int asset_id { get; set; }
        public int cagetory_id { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public Dictionary<string, object> attributes { get; set; }
        public string lifecycle_stage { get; set; } = string.Empty;
        public DateTime? installation_date { get; set; }
        public int expected_lifetime { get; set; }
        public string condition { get; set; } = string.Empty;
        public DateTime? last_inspection_date { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
