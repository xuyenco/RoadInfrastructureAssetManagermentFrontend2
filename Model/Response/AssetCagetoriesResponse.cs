
namespace Road_Infrastructure_Asset_Management.Model.Request
{
    public class AssetCagetoriesResponse
    {
        public int cagetory_id { get; set; }
        public string cagetory_name { get; set; }
        public string geometry_type { get; set; }
        public Dictionary<string, object> attributes_schema { get; set; } // JSON object
        public List<string> lifecycle_stages { get; set; }   // JSON array
        public DateTime? created_at { get; set; }
        public string marker_url { get; set; }
    }
}
