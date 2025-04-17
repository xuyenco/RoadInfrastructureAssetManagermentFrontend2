using Newtonsoft.Json.Linq;
using Road_Infrastructure_Asset_Management.Model.Geometry;

namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetsResponse
    {
        public int asset_id { get; set; }
        public int category_id { get; set; }
        public GeoJsonGeometry geometry { get; set; } = new GeoJsonGeometry();
        public string asset_name { get; set; }
        public string asset_code { get; set; }
        public string address { get; set; }
        public DateTime? construction_year { get; set; }
        public DateTime? operation_year { get; set; }
        public double? land_area { get; set; }
        public double? floor_area { get; set; }
        public double? original_value { get; set; }
        public double? remaining_value { get; set; }
        public string asset_status { get; set; }
        public string installation_unit { get; set; }
        public string management_unit { get; set; }
        public Dictionary<string,object> custom_attributes { get; set; }
        public DateTime? created_at { get; set; }
        public string image_url { get; set; }
    }
}
