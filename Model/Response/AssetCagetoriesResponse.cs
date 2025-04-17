
namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetCagetoriesResponse
    {
        public int category_id { get; set; }
        public string category_name { get; set; }
        public string geometry_type { get; set; }
        public Dictionary<string, object> attribute_schema { get; set; } // JSON object
        public DateTime? created_at { get; set; }
        public string sample_image { get; set; }
        public string icon_url { get; set; }
    }
}
