namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class AssetCagetoriesRequest
    {
        public string cagetory_name { get; set; }
        public string geometry_type { get; set; }
        public Dictionary<string, object> attributes_schema { get; set; } = new(); // JSON object
        public List<string> lifecycle_stages { get; set; } = new();   // JSON array
    }
}
