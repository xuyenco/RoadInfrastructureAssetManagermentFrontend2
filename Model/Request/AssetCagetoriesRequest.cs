namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class AssetCagetoriesRequest
    {
        public string cagetory_name { get; set; }
        public string geometry_type { get; set; }
        public string attributes_schema { get; set; } // Chuỗi JSON
        public string lifecycle_stages { get; set; } // Chuỗi JSON
        public IFormFile marker { get; set; }         // File ảnh
    }
}